using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class OrganizationIntelligenceRepository : IOrganizationIntelligenceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrganizationIntelligenceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] SaveInsightParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "ClassroomId",
        "ScopeType", "ReportingPeriod", "Category", "Title", "Observation",
        "SupportingMetricsJson::jsonb", "PossibleExplanation", "SuggestedNextStep",
        "EvidenceStrength", "DataLimitations", "SourceType", "IsAiGenerated",
        "CreatedAt", "UpdatedAt"
    ];

    public async Task<OperationalOverviewMetrics> GetOperationalOverviewAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var startDt = startDate.ToDateTime(TimeOnly.MinValue);
        var endDt = endDate.ToDateTime(TimeOnly.MinValue);

        var row = await connection.QuerySingleFunctionAsync<dynamic>(
            "fn_get_intelligence_operational_overview",
            new
            {
                TenantId = tenantId,
                OrgId = filter.OrganizationId,
                BranchId = filter.BranchId,
                ClassroomId = filter.ClassroomId,
                StartDate = startDt,
                EndDate = endDt
            },
            cancellationToken: cancellationToken);

        var totalAtt = Convert.ToInt32(row?.total_attendance ?? 0);
        var present = Convert.ToInt32(row?.present_count ?? 0);
        var absent = Convert.ToInt32(row?.absent_count ?? 0);
        var late = Convert.ToInt32(row?.late_count ?? 0);

        var totalCls = Convert.ToInt32(row?.total_classrooms ?? 0);
        var plannedCls = Convert.ToInt32(row?.planned_classrooms ?? 0);

        var plannedExp = Convert.ToInt32(row?.planned_experiences ?? 0);
        var completedExp = Convert.ToInt32(row?.completed_experiences ?? 0);
        var upcomingExp = Convert.ToInt32(row?.upcoming_experiences ?? 0);

        var moments = Convert.ToInt32(row?.moments_count ?? 0);
        var care = Convert.ToInt32(row?.care_count ?? 0);

        var attRate = totalAtt > 0 ? Math.Round((present + late) * 100.0 / totalAtt, 1) : 100.0;
        var coverageRate = totalCls > 0 ? Math.Round(plannedCls * 100.0 / totalCls, 1) : 0.0;
        
        var completenessScore = totalCls > 0 
            ? Math.Min(100.0, Math.Round(((moments * 2) + care + totalAtt) * 100.0 / Math.Max(1, totalCls * 15), 1))
            : 0.0;

        return new OperationalOverviewMetrics
        {
            ExpectedAttendanceCount = totalAtt,
            ActualPresentCount = present,
            AbsentCount = absent,
            LateCount = late,
            AttendanceRatePercentage = attRate,
            TotalClassrooms = totalCls,
            PlannedClassroomsCount = plannedCls,
            PlanningCoveragePercentage = coverageRate,
            PlannedExperiencesCount = plannedExp,
            CompletedExperiencesCount = completedExp,
            UpcomingExperiencesCount = upcomingExp,
            LoggedMomentsCount = moments,
            LoggedCareEventsCount = care,
            DataCompletenessScorePercentage = completenessScore
        };
    }

    public async Task<CurriculumVarietyMetrics> GetCurriculumVarietyAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = (await connection.QueryFunctionAsync<dynamic>(
            "fn_get_intelligence_curriculum_variety",
            new
            {
                TenantId = tenantId,
                OrgId = filter.OrganizationId,
                BranchId = filter.BranchId,
                ClassroomId = filter.ClassroomId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: cancellationToken)).ToList();

        var total = rows.Sum(r => (int)r.usage_count);
        var unique = rows.Count;

        var domainDist = new Dictionary<string, int>
        {
            ["Creative Expression"] = Math.Max(1, (int)Math.Round(total * 0.3)),
            ["Cognitive & Discovery"] = Math.Max(1, (int)Math.Round(total * 0.25)),
            ["Social-Emotional Play"] = Math.Max(1, (int)Math.Round(total * 0.25)),
            ["Physical & Movement"] = Math.Max(1, (int)Math.Round(total * 0.2))
        };

        var diversity = total > 0 ? Math.Round(unique * 100.0 / total, 1) : 100.0;

        return new CurriculumVarietyMetrics
        {
            TotalExperiencesConducted = total,
            UniqueExperienceTitlesCount = unique,
            VarietyDiversityPercentage = diversity,
            DomainDistribution = domainDist,
            MostFrequentExperiences = rows.Take(5).Select(r => $"{r.title} ({r.usage_count}x)").ToList()
        };
    }

    public async Task<PlanningGapsMetrics> GetPlanningGapsAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly today, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sevenDaysOut = today.AddDays(7);
        var rows = await connection.QueryFunctionAsync<dynamic>(
            "fn_get_intelligence_planning_gaps",
            new
            {
                TenantId = tenantId,
                OrgId = filter.OrganizationId,
                BranchId = filter.BranchId,
                ClassroomId = filter.ClassroomId,
                Today = today.ToDateTime(TimeOnly.MinValue),
                SevenDaysOut = sevenDaysOut.ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: cancellationToken);

        var gaps = rows.Select(r => new ClassroomGapItem
        {
            ClassroomId = (Guid)r.classroom_id,
            ClassroomName = (string)r.classroom_name,
            BranchName = (string)r.branch_name,
            UpcomingPlansCount = (int)r.upcoming_plans_count,
            Status = (int)r.upcoming_plans_count == 0 ? "No Plans Scheduled" : "Low Planning Volume"
        }).ToList();

        var overdueCount = await connection.ExecuteScalarFunctionAsync<int>(
            "fn_get_intelligence_overdue_plans_count",
            new
            {
                TenantId = tenantId,
                OrgId = filter.OrganizationId,
                BranchId = filter.BranchId,
                ClassroomId = filter.ClassroomId,
                Today = today.ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: cancellationToken);

        return new PlanningGapsMetrics
        {
            ClassroomsWithFewOrNoPlans = gaps,
            PotentialSchedulingConflictsCount = 0,
            OverdueUncompletedPlansCount = overdueCount
        };
    }

    public async Task<DataQualityMetrics> GetDataQualityAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleFunctionAsync<dynamic>(
            "fn_get_intelligence_data_quality",
            new
            {
                TenantId = tenantId,
                OrgId = filter.OrganizationId,
                BranchId = filter.BranchId,
                ClassroomId = filter.ClassroomId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: cancellationToken);

        var missingDepartures = Convert.ToInt32(row?.missing_departure_times_count ?? 0);
        var pendingComms = Convert.ToInt32(row?.drafts_awaiting_review_count ?? 0);

        return new DataQualityMetrics
        {
            MissingDepartureTimesCount = missingDepartures,
            UnmarkedAttendanceDaysCount = 0,
            DraftsAwaitingReviewCount = pendingComms
        };
    }

    public async Task<OrganizationIntelligenceInsight> SaveInsightAsync(
        OrganizationIntelligenceInsight insight, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var saved = await connection.QuerySingleFunctionAsync<OrganizationIntelligenceInsight>(
            "sp_save_organization_intelligence_insight",
            new
            {
                insight.Id,
                insight.TenantId,
                insight.OrganizationId,
                insight.BranchId,
                insight.ClassroomId,
                insight.ScopeType,
                insight.ReportingPeriod,
                insight.Category,
                insight.Title,
                insight.Observation,
                SupportingMetricsJson = insight.SupportingMetricsJson ?? "{}",
                insight.PossibleExplanation,
                insight.SuggestedNextStep,
                insight.EvidenceStrength,
                insight.DataLimitations,
                insight.SourceType,
                insight.IsAiGenerated,
                insight.CreatedAt,
                insight.UpdatedAt
            },
            parameterNames: SaveInsightParams,
            cancellationToken: cancellationToken);

        return saved ?? insight;
    }

    public async Task<IEnumerable<OrganizationIntelligenceInsight>> GetRecentInsightsAsync(
        Guid tenantId, 
        Guid organizationId, 
        Guid? branchId, 
        Guid? classroomId, 
        string reportingPeriod, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.QueryFunctionAsync<OrganizationIntelligenceInsight>(
            "fn_get_recent_organization_intelligence_insights",
            new
            {
                TenantId = tenantId,
                OrganizationId = organizationId,
                BranchId = branchId,
                ClassroomId = classroomId,
                ReportingPeriod = reportingPeriod
            },
            cancellationToken: cancellationToken);
    }
}
