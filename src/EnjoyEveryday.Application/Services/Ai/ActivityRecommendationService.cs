using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ActivityRecommendationService : IActivityRecommendationService
{
    private readonly IActivityRecommendationRepository _recommendationRepository;
    private readonly IMemoryService _memoryService;
    private readonly ExperienceService _experienceService;
    private readonly PlannerService _plannerService;
    private readonly ClassroomService _classroomService;
    private readonly ChildService _childService;
    private readonly TenantSettingsService _settingsService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly HttpClient _httpClient;
    private readonly ITeacherRepository? _teacherRepository;

    public ActivityRecommendationService(
        IActivityRecommendationRepository recommendationRepository,
        IMemoryService memoryService,
        ExperienceService experienceService,
        PlannerService plannerService,
        ClassroomService classroomService,
        ChildService childService,
        TenantSettingsService settingsService,
        ITenantContext tenantContext,
        IUserContext userContext,
        HttpClient httpClient,
        ITeacherRepository? teacherRepository = null)
    {
        _recommendationRepository = recommendationRepository;
        _memoryService = memoryService;
        _experienceService = experienceService;
        _plannerService = plannerService;
        _classroomService = classroomService;
        _childService = childService;
        _settingsService = settingsService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _httpClient = httpClient;
        _teacherRepository = teacherRepository;
    }

    public async Task<ActivityRecommendationResult> GenerateRecommendationsAsync(
        Guid classroomId,
        Guid? childId = null,
        RecommendationPreferencesDto? preferences = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Authorization & Scope Verification
        var classroom = await _classroomService.GetClassroomByIdAsync(classroomId);
        if (classroom == null)
            throw new ArgumentException("Classroom not found or unauthorized.", nameof(classroomId));

        string targetName = classroom.Name;
        Child? child = null;
        if (childId.HasValue && childId.Value != Guid.Empty)
        {
            var children = await _childService.GetChildrenByClassroomAsync(classroomId);
            child = children.FirstOrDefault(c => c.Id == childId.Value);
            if (child == null)
                throw new ArgumentException("Child not found in the selected classroom.", nameof(childId));
            targetName = $"{child.FirstName} {child.LastName}";
        }

        // 2. Retrieve authorized evidence from Memory Layer
        var queryParams = new MemoryQueryParameters
        {
            Limit = 30,
            StartDate = DateTimeOffset.UtcNow.AddMonths(-3)
        };

        IEnumerable<MemoryEvent> memoryEvents;
        if (child != null)
        {
            memoryEvents = await _memoryService.GetChildMemoriesAsync(child.Id, queryParams, cancellationToken);
        }
        else
        {
            memoryEvents = await _memoryService.GetClassroomMemoriesAsync(classroomId, queryParams, cancellationToken);
        }

        var memoryList = memoryEvents.ToList();

        // 3. Retrieve available experiences from Experience Library
        var allExperiences = (await _experienceService.GetExperiencesAsync()).ToList();
        var candidateExperiences = allExperiences.Where(e => e.Status == "Published" || e.Status == "Approved" || string.IsNullOrEmpty(e.Status) || e.Status == "Active").ToList();
        if (!candidateExperiences.Any())
        {
            candidateExperiences = allExperiences; // fallback to all
        }

        // 4. Query AI Agent or perform deterministic grounded matching
        var aiApiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
        List<RecommendationItemDto> aiItems = new();

        if (!string.IsNullOrEmpty(aiApiKey))
        {
            try
            {
                aiItems = await QueryAiAgentAsync(aiApiKey, child != null ? "Child" : "Classroom", targetName, candidateExperiences, memoryList, preferences, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActivityRecommendationService] AI Agent query failed: {ex.Message}. Falling back to deterministic matching.");
            }
        }

        // If AI returned empty or unavailable, perform deterministic grounded fallback
        if (!aiItems.Any())
        {
            aiItems = GenerateDeterministicRecommendations(child != null ? "Child" : "Classroom", targetName, candidateExperiences, memoryList, preferences);
        }

        // 5. Construct & persist recommendation request and items
        var request = new ActivityRecommendationRequest
        {
            TenantId = _tenantContext.TenantId,
            OrganizationId = _userContext.OrganizationId ?? Guid.Empty,
            ClassroomId = classroomId,
            ChildId = child?.Id,
            RequestedByUserId = _userContext.UserId,
            Scope = child != null ? "Child" : "Classroom",
            ContextPreferences = preferences != null ? JsonSerializer.Serialize(preferences) : null,
            EvidenceCount = memoryList.Count
        };

        var items = new List<ActivityRecommendationItem>();
        int rank = 1;
        foreach (var aiItem in aiItems.Take(5))
        {
            var matchedExp = candidateExperiences.FirstOrDefault(e => e.Id == aiItem.ExperienceId) ?? candidateExperiences.First();

            items.Add(new ActivityRecommendationItem
            {
                RequestId = request.Id,
                TenantId = _tenantContext.TenantId,
                ExperienceId = matchedExp.Id,
                ExperienceTitle = matchedExp.Title,
                SuggestedFormat = aiItem.SuggestedFormat ?? (child != null ? "Individual" : "Whole Class"),
                EstimatedDurationMinutes = aiItem.EstimatedDurationMinutes ?? 20,
                Rationale = aiItem.Rationale,
                SupportingEvidence = aiItem.SupportingEvidence != null ? JsonSerializer.Serialize(aiItem.SupportingEvidence) : null,
                Materials = aiItem.Materials != null ? JsonSerializer.Serialize(aiItem.Materials) : null,
                RankOrder = rank++,
                IsEvidenceLimited = aiItem.IsEvidenceLimited
            });
        }

        await _recommendationRepository.CreateRequestAsync(request, items, cancellationToken);
        request.Items = items;

        return new ActivityRecommendationResult(request, items);
    }

    public async Task<ActivityRecommendationDecision> AcceptRecommendationAsync(
        Guid requestId,
        Guid recommendationItemId,
        DateOnly scheduledDate,
        string timeOfDay,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        // Check for existing decision (Idempotency)
        var existingDecisions = await _recommendationRepository.GetDecisionsByRequestIdAsync(requestId, cancellationToken);
        var existing = existingDecisions.FirstOrDefault(d => d.RecommendationItemId == recommendationItemId && d.Decision == "Accepted");
        if (existing != null)
        {
            return existing;
        }

        var request = await _recommendationRepository.GetRequestByIdAsync(requestId, cancellationToken);
        if (request == null)
            throw new InvalidOperationException("Recommendation request not found.");

        var items = await _recommendationRepository.GetItemsByRequestIdAsync(requestId, cancellationToken);
        var item = items.FirstOrDefault(i => i.Id == recommendationItemId);
        if (item == null)
            throw new InvalidOperationException("Recommended item not found.");

        // Flow directly into existing Experience Planning engine
        var prepNotes = !string.IsNullOrWhiteSpace(notes) ? notes : $"Recommended based on evidence: {item.Rationale}";

        // Resolve valid Teacher ID if the current user is a registered teacher; otherwise null (FK constraint)
        Guid? teacherId = null;
        if (_teacherRepository != null)
        {
            try
            {
                var teachers = await _teacherRepository.GetAllAsync(_tenantContext.TenantId, cancellationToken);
                var teacher = teachers.FirstOrDefault(t => t.UserId == _userContext.UserId || t.Id == _userContext.UserId);
                teacherId = teacher?.Id;
            }
            catch
            {
                // Leave teacherId as null if resolution fails
            }
        }

        var schedule = await _plannerService.ScheduleExperienceAsync(
            item.ExperienceId,
            request.ClassroomId,
            scheduledDate,
            timeOfDay,
            teacherId,
            startTime,
            endTime,
            prepNotes);

        var decision = new ActivityRecommendationDecision
        {
            RequestId = requestId,
            RecommendationItemId = recommendationItemId,
            TenantId = _tenantContext.TenantId,
            DecidedByUserId = _userContext.UserId,
            Decision = "Accepted",
            ResultingScheduleId = schedule.Id,
            DecidedAt = DateTimeOffset.UtcNow
        };

        return await _recommendationRepository.RecordDecisionAsync(decision, cancellationToken);
    }

    public async Task<ActivityRecommendationDecision> RejectRecommendationAsync(
        Guid requestId,
        Guid recommendationItemId,
        string? rejectionReason = null,
        CancellationToken cancellationToken = default)
    {
        var decision = new ActivityRecommendationDecision
        {
            RequestId = requestId,
            RecommendationItemId = recommendationItemId,
            TenantId = _tenantContext.TenantId,
            DecidedByUserId = _userContext.UserId,
            Decision = "Rejected",
            RejectionReason = rejectionReason,
            DecidedAt = DateTimeOffset.UtcNow
        };

        return await _recommendationRepository.RecordDecisionAsync(decision, cancellationToken);
    }

    private async Task<List<RecommendationItemDto>> QueryAiAgentAsync(
        string apiKey,
        string scope,
        string targetName,
        List<Experience> candidates,
        List<MemoryEvent> memories,
        RecommendationPreferencesDto? preferences,
        CancellationToken cancellationToken)
    {
        var candidateDtos = candidates.Select(c => new
        {
            id = c.Id.ToString(),
            title = c.Title,
            description = c.Description,
            category = "Learning",
            materials = new List<string>()
        }).ToList();

        var memoryDtos = memories.Select(m => new
        {
            id = m.Id.ToString(),
            sourceId = m.SourceId.ToString(),
            sourceType = m.SourceType,
            eventType = m.EventType,
            title = m.Title,
            description = m.Description,
            eventDate = m.EventDate.ToString("yyyy-MM-dd")
        }).ToList();

        var requestBody = new
        {
            scope = scope,
            targetName = targetName,
            candidateExperiences = candidateDtos,
            evidenceMemories = memoryDtos,
            preferences = preferences != null ? new
            {
                preferredDurationMinutes = preferences.PreferredDurationMinutes,
                environment = preferences.Environment,
                format = preferences.Format,
                category = preferences.Category,
                additionalNotes = preferences.AdditionalNotes
            } : null
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8000/api/recommendations");
        message.Headers.Add("X-Tenant-Api-Key", apiKey);
        message.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(message, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = await response.Content.ReadFromJsonAsync<AiAgentResponseDto>(options, cancellationToken);
            if (raw?.Recommendations != null && raw.Recommendations.Any())
            {
                return raw.Recommendations;
            }
        }

        return new List<RecommendationItemDto>();
    }

    private List<RecommendationItemDto> GenerateDeterministicRecommendations(
        string scope,
        string targetName,
        List<Experience> candidates,
        List<MemoryEvent> memories,
        RecommendationPreferencesDto? preferences)
    {
        var results = new List<RecommendationItemDto>();
        bool hasEvidence = memories.Any();

        // 1. Score candidates based on objective, materials, variety, and memories
        var scoredCandidates = candidates.Select(exp =>
        {
            int score = 0;
            string matchReason = "";

            // Objective match
            if (!string.IsNullOrWhiteSpace(preferences?.LearningObjective))
            {
                var obj = preferences.LearningObjective.Trim();
                if ((!string.IsNullOrEmpty(exp.Description) && exp.Description.Contains(obj, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(exp.Title) && exp.Title.Contains(obj, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 15;
                    matchReason = $"Aligns directly with target objective '{obj}'. ";
                }
            }

            // Material match
            if (!string.IsNullOrWhiteSpace(preferences?.AvailableMaterials))
            {
                var mat = preferences.AvailableMaterials.Trim();
                if ((!string.IsNullOrEmpty(exp.Description) && exp.Description.Contains(mat, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(exp.DnaPayload) && exp.DnaPayload.Contains(mat, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 10;
                    matchReason += $"Utilizes requested materials ({mat}). ";
                }
            }

            // Category match
            if (!string.IsNullOrWhiteSpace(preferences?.Category) && preferences.Category != "All")
            {
                if ((!string.IsNullOrEmpty(exp.Description) && exp.Description.Contains(preferences.Category, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(exp.Title) && exp.Title.Contains(preferences.Category, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 8;
                }
            }

            // Related memory check
            var relatedMemory = memories.FirstOrDefault(m =>
                (!string.IsNullOrEmpty(m.Description) && exp.Title.Split(' ').Any(w => w.Length > 3 && m.Description.Contains(w, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(m.Title) && exp.Title.Contains(m.Title, StringComparison.OrdinalIgnoreCase)));

            if (relatedMemory != null)
            {
                score += 12;
            }

            return new { Experience = exp, Score = score, MatchReason = matchReason, RelatedMemory = relatedMemory };
        })
        .OrderByDescending(x => x.Score)
        .ThenBy(x => x.Experience.Title)
        .ToList();

        foreach (var item in scoredCandidates.Take(5))
        {
            var exp = item.Experience;
            string rationale;
            var supportingEvidence = new List<SupportingEvidenceCitationDto>();

            if (!string.IsNullOrEmpty(item.MatchReason))
            {
                rationale = $"{item.MatchReason}Suitable for {targetName}.";
            }
            else if (item.RelatedMemory != null)
            {
                rationale = $"Connects with observed interest in {item.RelatedMemory.Title} recorded on {item.RelatedMemory.EventDate:MMM dd}.";
                supportingEvidence.Add(new SupportingEvidenceCitationDto
                {
                    SourceId = item.RelatedMemory.SourceId.ToString(),
                    SourceType = item.RelatedMemory.SourceType,
                    Summary = $"{item.RelatedMemory.EventType}: {item.RelatedMemory.Title} ({item.RelatedMemory.EventDate:MMM dd})"
                });
            }
            else if (hasEvidence)
            {
                var topMemory = memories.First();
                rationale = $"Introduces engaging variety to complement recent sessions (last recorded: {topMemory.EventDate:MMM dd}).";
                supportingEvidence.Add(new SupportingEvidenceCitationDto
                {
                    SourceId = topMemory.SourceId.ToString(),
                    SourceType = topMemory.SourceType,
                    Summary = $"Recent session on {topMemory.EventDate:MMM dd}"
                });
            }
            else
            {
                rationale = $"Recommended foundational experience from your active catalog suitable for {targetName}.";
            }

            results.Add(new RecommendationItemDto
            {
                ExperienceId = exp.Id,
                ExperienceTitle = exp.Title,
                SuggestedFormat = scope == "Classroom" ? (preferences?.Format ?? "Whole Class") : "Individual",
                EstimatedDurationMinutes = preferences?.PreferredDurationMinutes ?? 20,
                Rationale = rationale,
                SupportingEvidence = supportingEvidence,
                Materials = new List<string> { "Standard classroom craft & learning supplies" },
                IsEvidenceLimited = !hasEvidence && string.IsNullOrEmpty(item.MatchReason)
            });
        }

        return results;
    }

    private class AiAgentResponseDto
    {
        public List<RecommendationItemDto>? Recommendations { get; set; }
    }

    public class RecommendationItemDto
    {
        public Guid ExperienceId { get; set; }
        public string ExperienceTitle { get; set; } = string.Empty;
        public string? SuggestedFormat { get; set; }
        public int? EstimatedDurationMinutes { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public List<SupportingEvidenceCitationDto>? SupportingEvidence { get; set; }
        public List<string>? Materials { get; set; }
        public bool IsEvidenceLimited { get; set; }
    }

    public class SupportingEvidenceCitationDto
    {
        public string? SourceId { get; set; }
        public string? SourceType { get; set; }
        public string? Summary { get; set; }
    }
}
