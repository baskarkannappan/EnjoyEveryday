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

public class WeeklyPlanningAssistantService : IWeeklyPlanningAssistantService
{
    private readonly IWeeklyPlanningRepository _repository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly ExperienceService _experienceService;
    private readonly PlannerService _plannerService;
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly TenantSettingsService _settingsService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeeklyPlanningAssistantService> _logger;

    public WeeklyPlanningAssistantService(
        IWeeklyPlanningRepository repository,
        IClassroomRepository classroomRepository,
        ExperienceService experienceService,
        PlannerService plannerService,
        IExperienceScheduleRepository scheduleRepository,
        TenantSettingsService settingsService,
        ITenantContext tenantContext,
        IUserContext userContext,
        HttpClient httpClient,
        ILogger<WeeklyPlanningAssistantService> logger)
    {
        _repository = repository;
        _classroomRepository = classroomRepository;
        _experienceService = experienceService;
        _plannerService = plannerService;
        _scheduleRepository = scheduleRepository;
        _settingsService = settingsService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeeklyPlanningDraftRequest?> GetLatestDraftForWeekAsync(Guid classroomId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _repository.GetLatestDraftForWeekAsync(tenantId, classroomId, weekStartDate, cancellationToken);
    }

    public async Task<WeeklyPlanningDraftRequest> GenerateWeeklyPlanDraftAsync(GenerateWeeklyPlanInput input, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        // 1. Authorize Classroom
        var classroom = await _classroomRepository.GetByIdAsync(tenantId, input.ClassroomId, cancellationToken);
        if (classroom == null)
        {
            throw new ArgumentException("Classroom not found or not authorized.", nameof(input.ClassroomId));
        }

        // Align Monday to Friday
        var monday = input.WeekStartDate;
        while (monday.DayOfWeek != DayOfWeek.Monday)
        {
            monday = monday.AddDays(-1);
        }
        var friday = monday.AddDays(4);

        // 2. Fetch candidate active experiences
        var allExperiences = (await _experienceService.GetExperiencesAsync(cancellationToken)).ToList();
        var candidateExperiences = allExperiences
            .Where(e => e.Status == "Active" || e.Status == "Published")
            .ToList();

        if (!candidateExperiences.Any())
        {
            throw new InvalidOperationException("No active experiences found in library to generate a weekly plan.");
        }

        // 3. Fetch recent schedules to avoid repetition
        var recentStart = monday.AddDays(-14);
        var recentSchedules = await _scheduleRepository.GetByClassroomAndDateRangeAsync(classroom.Id, recentStart, monday.AddDays(-1));
        var recentExpIds = recentSchedules.Select(s => s.ExperienceId).ToHashSet();
        var recentTitles = allExperiences.Where(e => recentExpIds.Contains(e.Id)).Select(e => e.Title).ToList();

        // 4. Fetch existing schedules for the target week to detect potential conflicts
        var existingWeekSchedules = (await _scheduleRepository.GetByClassroomAndDateRangeAsync(classroom.Id, monday, friday)).ToList();

        // 5. Build candidate payload
        var candidatePayload = candidateExperiences.Select(c => new
        {
            id = c.Id.ToString(),
            title = c.Title,
            description = c.Description,
            category = "Play & Discovery",
            materials = new List<string>()
        }).ToList();

        // 6. Call AI Agent
        AiWeeklyPlanResponse? aiResult = null;
        string? apiKey = null;
        try
        {
            apiKey = await _settingsService.GetAiApiKeyAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve AI API key for weekly planning");
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var payload = new
                {
                    classroomId = classroom.Id.ToString(),
                    classroomName = classroom.Name,
                    ageGroup = classroom.AgeGroup,
                    weekStartDate = monday.ToString("yyyy-MM-dd"),
                    weekEndDate = friday.ToString("yyyy-MM-dd"),
                    candidateExperiences = candidatePayload,
                    recentExperiences = recentTitles,
                    preferences = new
                    {
                        focusAreas = input.FocusAreas,
                        indoorOutdoorPreference = input.IndoorOutdoorPreference,
                        materialsNotes = input.AvailableMaterialsNotes
                    }
                };

                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "/api/weekly-plan");
                reqMsg.Headers.Add("X-Tenant-Api-Key", apiKey);
                reqMsg.Content = JsonContent.Create(payload);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                var response = await _httpClient.SendAsync(reqMsg, linkedCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    aiResult = await response.Content.ReadFromJsonAsync<AiWeeklyPlanResponse>(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error communicating with AI agent for weekly planning; using grounded deterministic fallback");
            }
        }

        // 7. Deterministic Fallback if AI unavailable or invalid
        if (aiResult == null || aiResult.DailyPlans == null || !aiResult.DailyPlans.Any())
        {
            aiResult = GenerateDeterministicWeeklyPlan(classroom, candidateExperiences, recentExpIds, input);
        }

        // 8. Construct Draft Request Record
        var draftRequest = new WeeklyPlanningDraftRequest
        {
            TenantId = tenantId,
            OrganizationId = _userContext.OrganizationId ?? Guid.Empty,
            ClassroomId = classroom.Id,
            RequestedByUserId = _userContext.UserId,
            WeekStartDate = monday,
            WeekEndDate = friday,
            FocusAreas = input.FocusAreas,
            IndoorOutdoorPreference = input.IndoorOutdoorPreference,
            MaterialsNotes = input.AvailableMaterialsNotes,
            Status = "Draft"
        };

        // 9. Validate items against scheduling conflicts & candidates
        var validExpMap = candidateExperiences.ToDictionary(e => e.Id);
        var draftItems = new List<WeeklyPlanningDraftItem>();
        var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

        for (int i = 0; i < 5; i++)
        {
            var date = monday.AddDays(i);
            var dayName = dayNames[i];
            var aiItem = aiResult.DailyPlans.FirstOrDefault(p => string.Equals(p.DayOfWeek, dayName, StringComparison.OrdinalIgnoreCase))
                         ?? aiResult.DailyPlans.ElementAtOrDefault(i);

            Guid expId = Guid.Empty;
            string expTitle = string.Empty;
            string? learningGoal = aiItem?.LearningGoal;
            string groupFormat = aiItem?.GroupFormat ?? "Small Group";
            int duration = aiItem?.EstimatedDurationMinutes ?? 25;
            string timeOfDay = aiItem?.TimeOfDay ?? "Morning";
            string? materials = aiItem?.RequiredMaterials;
            string? rationale = aiItem?.Rationale;

            if (aiItem != null && Guid.TryParse(aiItem.ExperienceId, out var parsedId) && validExpMap.TryGetValue(parsedId, out var matchedExp))
            {
                expId = matchedExp.Id;
                expTitle = matchedExp.Title;
            }
            else
            {
                // Fallback to candidate not recently used
                var chosen = candidateExperiences.FirstOrDefault(c => !recentExpIds.Contains(c.Id)) ?? candidateExperiences.First();
                expId = chosen.Id;
                expTitle = chosen.Title;
                learningGoal ??= $"Creative exploration in {chosen.Title}";
                rationale ??= $"Curated activity matching {classroom.Name}'s rhythm.";
            }

            // Conflict check with existing schedules
            var conflict = existingWeekSchedules.FirstOrDefault(s => s.ScheduledDate == date && s.TimeOfDay == timeOfDay && s.Status != "Cancelled");
            string validationStatus = "Valid";
            string? validationNotes = null;

            if (conflict != null)
            {
                validationStatus = "Warning";
                validationNotes = $"Classroom already has an experience scheduled ({conflict.TimeOfDay}). You can adjust time of day or replace.";
            }
            else if (recentExpIds.Contains(expId))
            {
                validationStatus = "Warning";
                validationNotes = "Activity was conducted recently in this classroom within the last 14 days.";
            }

            draftItems.Add(new WeeklyPlanningDraftItem
            {
                RequestId = draftRequest.Id,
                TenantId = tenantId,
                ScheduledDate = date,
                DayOfWeek = dayName,
                TimeOfDay = timeOfDay,
                ExperienceId = expId,
                ExperienceTitle = expTitle,
                LearningGoal = learningGoal,
                GroupFormat = groupFormat,
                EstimatedDurationMinutes = duration,
                RequiredMaterials = materials,
                Rationale = rationale,
                ValidationStatus = validationStatus,
                ValidationNotes = validationNotes
            });
        }

        // 10. Persist draft
        return await _repository.CreateDraftRequestAsync(draftRequest, draftItems, cancellationToken);
    }

    public async Task<WeeklyPlanningDraftItem> UpdateDraftItemAsync(WeeklyPlanningDraftItem item, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        item.TenantId = tenantId;

        // Revalidate item against library
        var exp = await _experienceService.GetExperienceByIdAsync(item.ExperienceId, cancellationToken);
        if (exp != null)
        {
            item.ExperienceTitle = exp.Title;
        }

        await _repository.UpdateDraftItemAsync(item, cancellationToken);
        return item;
    }

    public async Task<WeeklyPlanningDraftRequest> SavePlanToExperiencePlanningAsync(SubmitWeeklyPlanInput input, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var draft = await _repository.GetDraftByIdAsync(tenantId, input.DraftRequestId, cancellationToken);
        if (draft == null)
        {
            throw new KeyNotFoundException("Weekly planning draft not found.");
        }

        // Revalidate permissions
        if (!_userContext.HasPermission("experience.schedule"))
        {
            throw new UnauthorizedAccessException("Requires experience.schedule permission to schedule experiences.");
        }

        var itemsToSave = input.ConfirmedItems.Any() ? input.ConfirmedItems : draft.Items;

        foreach (var item in itemsToSave)
        {
            // Idempotent check: if already has ResultingScheduleId, skip creating duplicate
            if (item.ResultingScheduleId.HasValue)
            {
                var existing = await _scheduleRepository.GetByIdAsync(item.ResultingScheduleId.Value);
                if (existing != null)
                {
                    continue;
                }
            }

            // Schedule via existing PlannerService
            var schedule = await _plannerService.ScheduleExperienceAsync(
                experienceId: item.ExperienceId,
                classroomId: draft.ClassroomId,
                scheduledDate: item.ScheduledDate,
                timeOfDay: item.TimeOfDay,
                primaryTeacherId: _userContext.UserId,
                notes: $"{item.LearningGoal} [Weekly Assistant Rationale: {item.Rationale}]"
            );

            item.ResultingScheduleId = schedule.Id;
            item.ValidationStatus = "Valid";
            await _repository.UpdateDraftItemAsync(item, cancellationToken);

            if (input.AutoConfirmSchedules)
            {
                await _plannerService.ConfirmScheduleAsync(schedule.Id);
            }
        }

        await _repository.UpdateDraftStatusAsync(tenantId, draft.Id, "Approved", cancellationToken);
        draft.Status = "Approved";
        return draft;
    }

    private static AiWeeklyPlanResponse GenerateDeterministicWeeklyPlan(
        Classroom classroom, 
        List<Experience> candidates, 
        HashSet<Guid> recentExpIds, 
        GenerateWeeklyPlanInput input)
    {
        var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
        var available = candidates.Where(c => !recentExpIds.Contains(c.Id)).ToList();
        if (available.Count < 5) available = candidates;

        var plans = new List<AiDailyPlanDto>();
        for (int i = 0; i < 5; i++)
        {
            var exp = available[i % available.Count];
            plans.Add(new AiDailyPlanDto
            {
                DayOfWeek = dayNames[i],
                ExperienceId = exp.Id.ToString(),
                ExperienceTitle = exp.Title,
                LearningGoal = $"Encourage curiosity and engagement in {exp.Title}",
                GroupFormat = "Small Group",
                EstimatedDurationMinutes = 25,
                TimeOfDay = "Morning",
                RequiredMaterials = "Classroom materials",
                Rationale = $"Balanced curriculum activity suited for {classroom.Name} ({classroom.AgeGroup ?? "Pre-K"})."
            });
        }

        return new AiWeeklyPlanResponse
        {
            WeekSummary = $"Curated weekly plan draft for {classroom.Name} balancing active discovery, rhythm, and play.",
            DailyPlans = plans
        };
    }

    private class AiWeeklyPlanResponse
    {
        public string? WeekSummary { get; set; }
        public List<AiDailyPlanDto> DailyPlans { get; set; } = new();
    }

    private class AiDailyPlanDto
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public string ExperienceId { get; set; } = string.Empty;
        public string ExperienceTitle { get; set; } = string.Empty;
        public string? LearningGoal { get; set; }
        public string GroupFormat { get; set; } = "Small Group";
        public int EstimatedDurationMinutes { get; set; } = 25;
        public string TimeOfDay { get; set; } = "Morning";
        public string? RequiredMaterials { get; set; }
        public string? Rationale { get; set; }
    }
}
