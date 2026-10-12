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
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class OrganizationIntelligenceService : IOrganizationIntelligenceService
{
    private readonly IOrganizationIntelligenceRepository _repository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly TenantSettingsService _settingsService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationIntelligenceService> _logger;

    public OrganizationIntelligenceService(
        IOrganizationIntelligenceRepository repository,
        IOrganizationRepository organizationRepository,
        IBranchRepository branchRepository,
        IClassroomRepository classroomRepository,
        TenantSettingsService settingsService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        HttpClient httpClient,
        ILogger<OrganizationIntelligenceService> logger)
    {
        _repository = repository;
        _organizationRepository = organizationRepository;
        _branchRepository = branchRepository;
        _classroomRepository = classroomRepository;
        _settingsService = settingsService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrganizationIntelligenceReport> GetIntelligenceReportAsync(
        OrganizationIntelligenceFilter filter, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        // 1. Validate Organization Access
        var org = await _organizationRepository.GetByIdAsync(tenantId, filter.OrganizationId, cancellationToken);
        if (org == null)
        {
            throw new UnauthorizedAccessException($"Organization {filter.OrganizationId} not authorized or not found.");
        }

        string? branchName = null;
        if (filter.BranchId.HasValue)
        {
            var branch = await _branchRepository.GetByIdAsync(tenantId, filter.BranchId.Value, cancellationToken);
            if (branch == null || branch.OrganizationId != org.Id)
            {
                throw new UnauthorizedAccessException($"Branch {filter.BranchId.Value} is not authorized under organization {org.Id}.");
            }
            branchName = branch.Name;
        }

        string? classroomName = null;
        if (filter.ClassroomId.HasValue)
        {
            var cls = await _classroomRepository.GetByIdAsync(tenantId, filter.ClassroomId.Value, cancellationToken);
            if (cls == null)
            {
                throw new UnauthorizedAccessException($"Classroom {filter.ClassroomId.Value} is not authorized.");
            }
            classroomName = cls.Name;
        }

        // 2. Resolve Date Range
        var (startDate, endDate) = ResolveDateRange(filter.ReportingPeriod);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 3. Compute Metrics Deterministically
        var operational = await _repository.GetOperationalOverviewAsync(tenantId, filter, startDate, endDate, cancellationToken);
        var curriculum = await _repository.GetCurriculumVarietyAsync(tenantId, filter, startDate, endDate, cancellationToken);
        var planningGaps = await _repository.GetPlanningGapsAsync(tenantId, filter, today, cancellationToken);
        var dataQuality = await _repository.GetDataQualityAsync(tenantId, filter, startDate, endDate, cancellationToken);

        // 4. Retrieve or Generate Insights
        var recentInsights = (await _repository.GetRecentInsightsAsync(
            tenantId, 
            filter.OrganizationId, 
            filter.BranchId, 
            filter.ClassroomId, 
            filter.ReportingPeriod, 
            cancellationToken)).ToList();

        if (!recentInsights.Any())
        {
            // Auto-generate fresh insights for this filter
            return await GenerateInsightsInternalAsync(org, branchName, classroomName, filter, operational, curriculum, planningGaps, dataQuality, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            recentInsights = recentInsights.Where(i => string.Equals(i.Category, filter.Category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return new OrganizationIntelligenceReport
        {
            Filter = filter,
            OrganizationName = org.Name,
            BranchName = branchName,
            ClassroomName = classroomName,
            OperationalOverview = operational,
            CurriculumVariety = curriculum,
            PlanningGaps = planningGaps,
            DataQuality = dataQuality,
            Insights = recentInsights,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<OrganizationIntelligenceReport> GenerateInsightsAsync(
        OrganizationIntelligenceFilter filter, 
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var org = await _organizationRepository.GetByIdAsync(tenantId, filter.OrganizationId, cancellationToken);
        if (org == null)
        {
            throw new UnauthorizedAccessException($"Organization {filter.OrganizationId} not authorized.");
        }

        string? branchName = null;
        if (filter.BranchId.HasValue)
        {
            var branch = await _branchRepository.GetByIdAsync(tenantId, filter.BranchId.Value, cancellationToken);
            branchName = branch?.Name;
        }

        string? classroomName = null;
        if (filter.ClassroomId.HasValue)
        {
            var cls = await _classroomRepository.GetByIdAsync(tenantId, filter.ClassroomId.Value, cancellationToken);
            classroomName = cls?.Name;
        }

        var (startDate, endDate) = ResolveDateRange(filter.ReportingPeriod);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var operational = await _repository.GetOperationalOverviewAsync(tenantId, filter, startDate, endDate, cancellationToken);
        var curriculum = await _repository.GetCurriculumVarietyAsync(tenantId, filter, startDate, endDate, cancellationToken);
        var planningGaps = await _repository.GetPlanningGapsAsync(tenantId, filter, today, cancellationToken);
        var dataQuality = await _repository.GetDataQualityAsync(tenantId, filter, startDate, endDate, cancellationToken);

        return await GenerateInsightsInternalAsync(org, branchName, classroomName, filter, operational, curriculum, planningGaps, dataQuality, cancellationToken);
    }

    private async Task<OrganizationIntelligenceReport> GenerateInsightsInternalAsync(
        Organization org,
        string? branchName,
        string? classroomName,
        OrganizationIntelligenceFilter filter,
        OperationalOverviewMetrics operational,
        CurriculumVarietyMetrics curriculum,
        PlanningGapsMetrics planningGaps,
        DataQualityMetrics dataQuality,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var scopeName = classroomName ?? branchName ?? org.Name;
        var scopeType = filter.ClassroomId.HasValue ? "Classroom" : (filter.BranchId.HasValue ? "Branch" : "Organization");

        // Determine evidence strength based on sample size & data completeness
        var evidenceStrength = operational.DataCompletenessScorePercentage switch
        {
            >= 70.0 => EvidenceStrengthLevel.Strong,
            >= 30.0 => EvidenceStrengthLevel.Limited,
            _ => EvidenceStrengthLevel.Insufficient
        };

        var rawInsights = await CallAiServiceOrFallbackAsync(scopeName, filter.ReportingPeriod, operational, curriculum, planningGaps, dataQuality, cancellationToken);

        var savedInsights = new List<OrganizationIntelligenceInsight>();
        foreach (var raw in rawInsights)
        {
            var insight = new OrganizationIntelligenceInsight
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrganizationId = filter.OrganizationId,
                BranchId = filter.BranchId,
                ClassroomId = filter.ClassroomId,
                ScopeType = scopeType,
                ReportingPeriod = filter.ReportingPeriod,
                Category = raw.Category ?? OrganizationIntelligenceCategory.OperationalOverview,
                Title = raw.Title ?? "Executive Operational Observation",
                Observation = raw.Observation ?? "Standard operational cadence observed.",
                SupportingMetricsJson = JsonSerializer.Serialize(new
                {
                    operational.AttendanceRatePercentage,
                    operational.PlanningCoveragePercentage,
                    operational.CompletedExperiencesCount,
                    curriculum.VarietyDiversityPercentage,
                    planningGaps.ClassroomsWithFewOrNoPlans.Count,
                    dataQuality.TotalActionableItems
                }),
                PossibleExplanation = raw.PossibleExplanation,
                SuggestedNextStep = raw.SuggestedNextStep,
                EvidenceStrength = evidenceStrength,
                DataLimitations = raw.DataLimitations ?? "Based on recorded activities and attendance logs.",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var saved = await _repository.SaveInsightAsync(insight, cancellationToken);
            savedInsights.Add(saved);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "OrganizationIntelligence.InsightsGenerated",
            EntityType = "OrganizationIntelligenceInsight",
            EntityId = filter.OrganizationId,
            Details = $"Generated {savedInsights.Count} insights for {scopeName} ({filter.ReportingPeriod})"
        }, cancellationToken);

        return new OrganizationIntelligenceReport
        {
            Filter = filter,
            OrganizationName = org.Name,
            BranchName = branchName,
            ClassroomName = classroomName,
            OperationalOverview = operational,
            CurriculumVariety = curriculum,
            PlanningGaps = planningGaps,
            DataQuality = dataQuality,
            Insights = savedInsights,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<List<AiInsightDto>> CallAiServiceOrFallbackAsync(
        string scopeName,
        string reportingPeriod,
        OperationalOverviewMetrics operational,
        CurriculumVarietyMetrics curriculum,
        PlanningGapsMetrics planningGaps,
        DataQualityMetrics dataQuality,
        CancellationToken cancellationToken)
    {
        var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var payload = new
                {
                    scopeName,
                    reportingPeriod,
                    attendanceRate = operational.AttendanceRatePercentage,
                    planningCoverage = operational.PlanningCoveragePercentage,
                    completedExperiences = operational.CompletedExperiencesCount,
                    upcomingExperiences = operational.UpcomingExperiencesCount,
                    uniqueExperiences = curriculum.UniqueExperienceTitlesCount,
                    varietyDiversityRate = curriculum.VarietyDiversityPercentage,
                    domainBreakdown = curriculum.DomainDistribution,
                    classroomsNeedingPlansCount = planningGaps.ClassroomsWithFewOrNoPlans.Count,
                    classroomsNeedingPlans = planningGaps.ClassroomsWithFewOrNoPlans.Select(c => c.ClassroomName).ToList(),
                    missingDeparturesCount = dataQuality.MissingDepartureTimesCount,
                    pendingDraftsCount = dataQuality.DraftsAwaitingReviewCount,
                    dataCompletenessScore = operational.DataCompletenessScorePercentage
                };

                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "/api/organization-intelligence");
                reqMsg.Headers.Add("X-Tenant-Api-Key", apiKey);
                reqMsg.Content = JsonContent.Create(payload);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                var response = await _httpClient.SendAsync(reqMsg, linkedCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<AiInsightDto>>(cancellationToken: cancellationToken);
                    if (result != null && result.Any())
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invoking AI service for organization intelligence; using deterministic fallback.");
            }
        }

        return BuildDeterministicFallback(scopeName, operational, curriculum, planningGaps, dataQuality);
    }

    private static List<AiInsightDto> BuildDeterministicFallback(
        string scopeName,
        OperationalOverviewMetrics operational,
        CurriculumVarietyMetrics curriculum,
        PlanningGapsMetrics planningGaps,
        DataQualityMetrics dataQuality)
    {
        var list = new List<AiInsightDto>();

        // 1. Operational Overview
        list.Add(new AiInsightDto
        {
            Title = "Operational Attendance & Classroom Rhythm",
            Category = OrganizationIntelligenceCategory.OperationalOverview,
            Observation = $"Attendance rate is {operational.AttendanceRatePercentage}% across {operational.TotalClassrooms} classrooms, with {operational.CompletedExperiencesCount} completed experiences.",
            PossibleExplanation = "High attendance and steady completion numbers reflect a healthy, stable operating rhythm across classrooms.",
            SuggestedNextStep = "Review arrival peaks during morning check-in to ensure optimal educator-to-child ratios.",
            DataLimitations = "Calculated from confirmed attendance records logged in the system."
        });

        // 2. Curriculum Variety
        var topDomain = curriculum.DomainDistribution.OrderByDescending(d => d.Value).FirstOrDefault().Key ?? "Play & Discovery";
        list.Add(new AiInsightDto
        {
            Title = "Curriculum Balance & Learning Domains",
            Category = OrganizationIntelligenceCategory.CurriculumVariety,
            Observation = $"Classrooms engaged in {curriculum.UniqueExperienceTitlesCount} unique activities ({curriculum.VarietyDiversityPercentage}% variety score), with strong activity in {topDomain}.",
            PossibleExplanation = "Teachers are incorporating varied sensory, cognitive, and cooperative experiences into classroom schedules.",
            SuggestedNextStep = "Encourage educators to explore less frequent learning areas (like outdoor nature exploration) in next week's plans.",
            DataLimitations = "Domain counts are inferred from tags in scheduled experiences."
        });

        // 3. Planning Gaps
        if (planningGaps.ClassroomsWithFewOrNoPlans.Any())
        {
            var clsNames = string.Join(", ", planningGaps.ClassroomsWithFewOrNoPlans.Take(3).Select(c => c.ClassroomName));
            list.Add(new AiInsightDto
            {
                Title = "Advance Planning Coverage",
                Category = OrganizationIntelligenceCategory.PlanningGaps,
                Observation = $"{planningGaps.ClassroomsWithFewOrNoPlans.Count} classroom(s) ({clsNames}) currently have fewer than 2 experiences scheduled for the coming week.",
                PossibleExplanation = "Educators may be preparing plans manually or waiting for weekly theme approvals.",
                SuggestedNextStep = "Remind lead teachers to utilize the AI Weekly Planning Assistant to prepare balanced drafts in advance.",
                DataLimitations = "Only accounts for scheduled calendar entries; spontaneous daily play is not captured."
            });
        }

        // 4. Data Quality
        if (dataQuality.TotalActionableItems > 0)
        {
            list.Add(new AiInsightDto
            {
                Title = "Operational Record Completeness",
                Category = OrganizationIntelligenceCategory.DataQuality,
                Observation = $"Found {dataQuality.MissingDepartureTimesCount} attendance records missing departure timestamps and {dataQuality.DraftsAwaitingReviewCount} parent communication drafts awaiting review.",
                PossibleExplanation = "Afternoon transition rush can lead to deferred checkout entries and delayed communication approvals.",
                SuggestedNextStep = "Standardize the afternoon handover checklist so all children are signed out and parent drafts approved before closure.",
                DataLimitations = "Calculated based on active attendance checkouts and pending communication drafts."
            });
        }

        return list;
    }

    private static (DateOnly StartDate, DateOnly EndDate) ResolveDateRange(string reportingPeriod)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return reportingPeriod switch
        {
            IntelligenceReportingPeriod.Last30Days => (today.AddDays(-30), today),
            IntelligenceReportingPeriod.ThisMonth => (new DateOnly(today.Year, today.Month, 1), today),
            _ => (today.AddDays(-7), today) // Last7Days default
        };
    }

    private class AiInsightDto
    {
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? Observation { get; set; }
        public string? PossibleExplanation { get; set; }
        public string? SuggestedNextStep { get; set; }
        public string? DataLimitations { get; set; }
    }
}
