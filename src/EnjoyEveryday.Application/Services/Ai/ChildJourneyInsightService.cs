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
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class ChildJourneyInsightService : IChildJourneyInsightService
{
    private readonly IChildJourneyInsightRepository _repository;
    private readonly IMemoryService _memoryService;
    private readonly ChildService _childService;
    private readonly ExperienceService _experienceService;
    private readonly TenantSettingsService _settingsService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChildJourneyInsightService> _logger;

    public ChildJourneyInsightService(
        IChildJourneyInsightRepository repository,
        IMemoryService memoryService,
        ChildService childService,
        ExperienceService experienceService,
        TenantSettingsService settingsService,
        ITenantContext tenantContext,
        IUserContext userContext,
        HttpClient httpClient,
        ILogger<ChildJourneyInsightService> logger)
    {
        _repository = repository;
        _memoryService = memoryService;
        _childService = childService;
        _experienceService = experienceService;
        _settingsService = settingsService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ChildJourneyInsightRequest?> GetLatestInsightForChildAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _repository.GetLatestInsightForChildAsync(tenantId, childId, cancellationToken);
    }

    public async Task<ChildJourneyInsightRequest> GenerateJourneyInsightsAsync(GenerateJourneyInsightInput input, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        // 1. Authorize & fetch child
        var child = await _childService.GetChildByIdAsync(input.ChildId, cancellationToken);
        if (child == null || child.TenantId != tenantId)
        {
            throw new ArgumentException("Child not found or not authorized.", nameof(input.ChildId));
        }

        if (!child.ClassroomId.HasValue)
        {
            throw new InvalidOperationException("Child must be assigned to a classroom to review progress insights.");
        }

        // 2. Resolve reporting period bounds
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly startDate;
        DateOnly endDate = today;

        switch (input.PeriodType)
        {
            case "Last7Days":
                startDate = today.AddDays(-7);
                break;
            case "CurrentMonth":
                startDate = new DateOnly(today.Year, today.Month, 1);
                break;
            case "Custom":
                startDate = input.CustomStartDate ?? today.AddDays(-30);
                endDate = input.CustomEndDate ?? today;
                if (endDate < startDate)
                {
                    (startDate, endDate) = (endDate, startDate);
                }
                break;
            case "Last30Days":
            default:
                input.PeriodType = "Last30Days";
                startDate = today.AddDays(-30);
                break;
        }

        // 3. Retrieve memories from the SQL Memory Layer
        var startDateTime = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endDateTime = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var queryParams = new MemoryQueryParameters
        {
            StartDate = startDateTime,
            EndDate = endDateTime,
            Limit = 50,
            Offset = 0
        };

        var rawMemories = await _memoryService.GetChildMemoriesAsync(child.Id, queryParams, cancellationToken);
        var memoryList = rawMemories.ToList();

        // 4. Retrieve candidate experiences for educational suggestion linking
        var allExperiences = await _experienceService.GetExperiencesAsync(cancellationToken);
        var activeExperiences = allExperiences.Where(e => e.Status == "Active" || e.Status == "Published").ToList();

        // 5. Build structured evidence package
        var evidenceItems = memoryList.Select(m => new
        {
            id = m.Id.ToString(),
            sourceId = m.SourceId.ToString(),
            sourceType = m.SourceType,
            eventType = m.EventType,
            title = m.Title,
            description = m.Description,
            eventDate = m.EventDate.ToString("yyyy-MM-dd")
        }).ToList();

        var candidateItems = activeExperiences.Take(15).Select(e => new
        {
            id = e.Id.ToString(),
            title = e.Title,
            description = e.Description,
            materials = new List<string>()
        }).ToList();

        // 6. Invoke AI Agent or fallback
        AiJourneyInsightResult? aiResult = null;
        string? apiKey = null;

        try
        {
            apiKey = await _settingsService.GetAiApiKeyAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve AI API key for tenant {TenantId}", tenantId);
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var payload = new
                {
                    childId = child.Id.ToString(),
                    childName = child.FirstName,
                    periodType = input.PeriodType,
                    periodStartDate = startDate.ToString("yyyy-MM-dd"),
                    periodEndDate = endDate.ToString("yyyy-MM-dd"),
                    focusArea = input.FocusArea,
                    teacherQuestion = input.TeacherQuestion,
                    evidenceMemories = evidenceItems,
                    candidateExperiences = candidateItems
                };

                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "/api/journey-insights");
                reqMsg.Headers.Add("X-Tenant-Api-Key", apiKey);
                reqMsg.Content = JsonContent.Create(payload);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                var response = await _httpClient.SendAsync(reqMsg, linkedCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    aiResult = await response.Content.ReadFromJsonAsync<AiJourneyInsightResult>(cancellationToken: cancellationToken);
                }
                else
                {
                    _logger.LogWarning("AI agent returned status {StatusCode} for child journey insights", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error communicating with AI agent for child {ChildId}; using grounded fallback", child.Id);
            }
        }

        // 7. Grounded deterministic fallback if AI is unavailable
        if (aiResult == null || aiResult.Items == null || !aiResult.Items.Any())
        {
            aiResult = GenerateDeterministicGroundedFallback(child, startDate, endDate, memoryList, activeExperiences, input.FocusArea, input.TeacherQuestion);
        }

        // 8. Sanitize and post-validate evidence citations
        var validMemorySourceIds = memoryList.Select(m => m.SourceId.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validExperienceIds = activeExperiences.Select(e => e.Id).ToHashSet();

        var requestRecord = new ChildJourneyInsightRequest
        {
            TenantId = tenantId,
            OrganizationId = _userContext.OrganizationId ?? Guid.Empty,
            ClassroomId = child.ClassroomId.Value,
            ChildId = child.Id,
            RequestedByUserId = _userContext.UserId,
            PeriodType = input.PeriodType,
            PeriodStartDate = startDate,
            PeriodEndDate = endDate,
            FocusArea = input.FocusArea,
            TeacherQuestion = input.TeacherQuestion,
            EvidenceCount = memoryList.Count,
            OverallSummary = aiResult.OverallSummary,
            EvidenceGaps = aiResult.EvidenceGaps
        };

        var items = new List<ChildJourneyInsightItem>();
        int rank = 1;

        foreach (var aiItem in aiResult.Items)
        {
            // Validate citations
            var citations = new List<ChildJourneyEvidenceCitation>();
            if (aiItem.SupportingEvidence != null)
            {
                foreach (var ev in aiItem.SupportingEvidence)
                {
                    if (validMemorySourceIds.Contains(ev.SourceId))
                    {
                        if (Guid.TryParse(ev.SourceId, out var parsedGuid))
                        {
                            citations.Add(new ChildJourneyEvidenceCitation
                            {
                                SourceId = parsedGuid,
                                SourceType = ev.SourceType,
                                Title = ev.Title,
                                Summary = ev.Summary,
                                EventDate = ev.EventDate
                            });
                        }
                    }
                }
            }

            Guid? suggestedExpId = null;
            string? suggestedExpTitle = null;
            if (aiItem.SuggestedExperienceId.HasValue && validExperienceIds.Contains(aiItem.SuggestedExperienceId.Value))
            {
                suggestedExpId = aiItem.SuggestedExperienceId.Value;
                suggestedExpTitle = activeExperiences.FirstOrDefault(e => e.Id == suggestedExpId)?.Title ?? aiItem.SuggestedExperienceTitle;
            }

            items.Add(new ChildJourneyInsightItem
            {
                RequestId = requestRecord.Id,
                TenantId = tenantId,
                Category = SanitizeCategory(aiItem.Category),
                Classification = SanitizeClassification(aiItem.Classification),
                Statement = aiItem.Statement,
                EvidenceStrength = string.IsNullOrWhiteSpace(aiItem.EvidenceStrength) ? "Supported" : aiItem.EvidenceStrength,
                SupportingEvidence = citations.Any() ? JsonSerializer.Serialize(citations) : null,
                SuggestedExperienceId = suggestedExpId,
                SuggestedExperienceTitle = suggestedExpTitle,
                RankOrder = rank++
            });
        }

        // 9. Persist and return
        return await _repository.CreateRequestAsync(requestRecord, items, cancellationToken);
    }

    private static string SanitizeCategory(string? category)
    {
        return category switch
        {
            "JourneySummary" => "JourneySummary",
            "RecordedInterests" => "RecordedInterests",
            "ParticipationPatterns" => "ParticipationPatterns",
            "LearningOpportunities" => "LearningOpportunities",
            "IdeasToConsider" => "IdeasToConsider",
            "EvidenceLimitations" => "EvidenceLimitations",
            _ => "JourneySummary"
        };
    }

    private static string SanitizeClassification(string? classification)
    {
        return classification switch
        {
            "RecordedFact" => "RecordedFact",
            "ObservedPattern" => "ObservedPattern",
            "EducationalSuggestion" => "EducationalSuggestion",
            "EvidenceLimitations" => "EvidenceLimitations",
            _ => "RecordedFact"
        };
    }

    private static AiJourneyInsightResult GenerateDeterministicGroundedFallback(
        Child child, 
        DateOnly startDate, 
        DateOnly endDate, 
        List<MemoryEvent> memories, 
        List<Experience> candidates,
        string? focusArea = null,
        string? teacherQuestion = null)
    {
        var items = new List<AiJourneyInsightItemDto>();
        int rank = 1;

        if (memories.Any())
        {
            var first = memories.First();
            var strength = memories.Count > 1 ? "Pattern supported by multiple records" : "Single observation";

            items.Add(new AiJourneyInsightItemDto
            {
                Category = "JourneySummary",
                Classification = "RecordedFact",
                Statement = $"{child.FirstName} has {memories.Count} recorded observations and experiences during this period.",
                EvidenceStrength = strength,
                SupportingEvidence = new List<AiJourneyEvidenceCitationDto>
                {
                    new()
                    {
                        SourceId = first.SourceId.ToString(),
                        SourceType = first.SourceType,
                        Title = first.Title,
                        Summary = first.Description
                    }
                },
                RankOrder = rank++
            });

            // Specific teacher question reflection
            if (!string.IsNullOrWhiteSpace(teacherQuestion))
            {
                var relevantMemories = memories.Where(m => 
                    m.Title.Contains(teacherQuestion, StringComparison.OrdinalIgnoreCase) ||
                    m.Description.Contains(teacherQuestion, StringComparison.OrdinalIgnoreCase) ||
                    (m.Category != null && teacherQuestion.Contains(m.Category, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (relevantMemories.Any())
                {
                    items.Add(new AiJourneyInsightItemDto
                    {
                        Category = "LearningOpportunities",
                        Classification = "ObservedPattern",
                        Statement = $"In response to teacher inquiry regarding '{teacherQuestion}': {relevantMemories.Count} matching record(s) confirm active engagement.",
                        EvidenceStrength = relevantMemories.Count > 1 ? "Pattern supported by multiple records" : "Single observation",
                        SupportingEvidence = relevantMemories.Take(3).Select(m => new AiJourneyEvidenceCitationDto
                        {
                            SourceId = m.SourceId.ToString(),
                            SourceType = m.SourceType,
                            Title = m.Title,
                            Summary = m.Description
                        }).ToList(),
                        RankOrder = rank++
                    });
                }
                else
                {
                    items.Add(new AiJourneyInsightItemDto
                    {
                        Category = "EvidenceLimitations",
                        Classification = "EvidenceLimitations",
                        Statement = $"In response to teacher inquiry regarding '{teacherQuestion}': No explicit observations directly address this inquiry in the selected timeframe.",
                        EvidenceStrength = "Insufficient evidence",
                        SupportingEvidence = new List<AiJourneyEvidenceCitationDto>(),
                        RankOrder = rank++
                    });
                }
            }

            // Learning Opportunity / Focus Area
            if (!string.IsNullOrWhiteSpace(focusArea) && focusArea != "All")
            {
                var focusMemories = memories.Where(m => 
                    m.Title.Contains(focusArea, StringComparison.OrdinalIgnoreCase) ||
                    m.Description.Contains(focusArea, StringComparison.OrdinalIgnoreCase) ||
                    (m.Category != null && m.Category.Contains(focusArea, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (focusMemories.Any())
                {
                    items.Add(new AiJourneyInsightItemDto
                    {
                        Category = "LearningOpportunities",
                        Classification = "ObservedPattern",
                        Statement = $"Focus on {focusArea}: Observed in {focusMemories.Count} moment(s), highlighting recorded progress and engagement.",
                        EvidenceStrength = focusMemories.Count > 1 ? "Pattern supported by multiple records" : "Single observation",
                        SupportingEvidence = focusMemories.Take(2).Select(m => new AiJourneyEvidenceCitationDto
                        {
                            SourceId = m.SourceId.ToString(),
                            SourceType = m.SourceType,
                            Title = m.Title,
                            Summary = m.Description
                        }).ToList(),
                        RankOrder = rank++
                    });
                }
            }

            // If there are observations about activities
            var story = memories.FirstOrDefault(m => m.SourceType == "child_stories" || m.SourceType == "child_journey_entries");
            if (story != null)
            {
                items.Add(new AiJourneyInsightItemDto
                {
                    Category = "RecordedInterests",
                    Classification = "RecordedFact",
                    Statement = $"Recorded observation reveals engagement in: {story.Title}.",
                    EvidenceStrength = "Single observation",
                    SupportingEvidence = new List<AiJourneyEvidenceCitationDto>
                    {
                        new()
                        {
                            SourceId = story.SourceId.ToString(),
                            SourceType = story.SourceType,
                            Title = story.Title,
                            Summary = story.Description
                        }
                    },
                    RankOrder = rank++
                });
            }

            // Participation pattern
            var sessions = memories.Where(m => m.SourceType == "experience_participations").ToList();
            if (sessions.Any())
            {
                items.Add(new AiJourneyInsightItemDto
                {
                    Category = "ParticipationPatterns",
                    Classification = "ObservedPattern",
                    Statement = $"{child.FirstName} actively participated in {sessions.Count} guided classroom experience session(s).",
                    EvidenceStrength = sessions.Count > 1 ? "Pattern supported by multiple records" : "Single observation",
                    SupportingEvidence = sessions.Take(2).Select(s => new AiJourneyEvidenceCitationDto
                    {
                        SourceId = s.SourceId.ToString(),
                        SourceType = s.SourceType,
                        Title = s.Title,
                        Summary = s.Description
                    }).ToList(),
                    RankOrder = rank++
                });
            }
        }
        else
        {
            items.Add(new AiJourneyInsightItemDto
            {
                Category = "JourneySummary",
                Classification = "RecordedFact",
                Statement = $"No observations or experience sessions were recorded for {child.FirstName} between {startDate} and {endDate}.",
                EvidenceStrength = "Limited evidence",
                SupportingEvidence = new List<AiJourneyEvidenceCitationDto>(),
                RankOrder = rank++
            });
        }

        // Ideas to Consider (Next Steps)
        if (candidates.Any())
        {
            Experience? chosenCand = null;
            if (!string.IsNullOrWhiteSpace(focusArea) && focusArea != "All")
            {
                chosenCand = candidates.FirstOrDefault(c => 
                    c.Title.Contains(focusArea, StringComparison.OrdinalIgnoreCase) ||
                    (c.Description != null && c.Description.Contains(focusArea, StringComparison.OrdinalIgnoreCase))
                );
            }
            chosenCand ??= candidates.First();

            items.Add(new AiJourneyInsightItemDto
            {
                Category = "IdeasToConsider",
                Classification = "EducationalSuggestion",
                Statement = $"Consider exploring '{chosenCand.Title}' to provide additional opportunities for discovery and skill practice.",
                EvidenceStrength = "Single observation",
                SupportingEvidence = new List<AiJourneyEvidenceCitationDto>(),
                SuggestedExperienceId = chosenCand.Id,
                SuggestedExperienceTitle = chosenCand.Title,
                RankOrder = rank++
            });
        }

        // Evidence Limitations item
        items.Add(new AiJourneyInsightItemDto
        {
            Category = "EvidenceLimitations",
            Classification = "EvidenceLimitations",
            Statement = "Administrative records (attendance and daily care) are tracked for wellbeing and are not interpreted as developmental mastery. Absence of observations in a learning domain indicates an opportunity for future observation rather than a child deficit.",
            EvidenceStrength = "Limited evidence",
            SupportingEvidence = new List<AiJourneyEvidenceCitationDto>(),
            RankOrder = rank++
        });

        return new AiJourneyInsightResult
        {
            OverallSummary = $"{child.FirstName}'s records reflect {memories.Count} moments captured between {startDate:MMM d} and {endDate:MMM d, yyyy}.",
            EvidenceGaps = memories.Count < 3 ? "Record history is limited for this date range; ongoing observations will deepen future insights." : null,
            Items = items
        };
    }

    private class AiJourneyInsightResult
    {
        public string? OverallSummary { get; set; }
        public string? EvidenceGaps { get; set; }
        public List<AiJourneyInsightItemDto> Items { get; set; } = new();
    }

    private class AiJourneyInsightItemDto
    {
        public string Category { get; set; } = "JourneySummary";
        public string Classification { get; set; } = "RecordedFact";
        public string Statement { get; set; } = string.Empty;
        public string? EvidenceStrength { get; set; }
        public List<AiJourneyEvidenceCitationDto>? SupportingEvidence { get; set; }
        public Guid? SuggestedExperienceId { get; set; }
        public string? SuggestedExperienceTitle { get; set; }
        public int RankOrder { get; set; } = 1;
    }

    private class AiJourneyEvidenceCitationDto
    {
        public string SourceId { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? EventDate { get; set; }
    }
}
