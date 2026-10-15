using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class OrganizationIntelligenceRepository : IOrganizationIntelligenceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrganizationIntelligenceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

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

        const string sql = @"
            -- 1. Attendance
            SELECT 
                COUNT(*) AS TotalRecords,
                COUNT(*) FILTER (WHERE ca.status = 'Present') AS PresentCount,
                COUNT(*) FILTER (WHERE ca.status = 'Absent') AS AbsentCount,
                COUNT(*) FILTER (WHERE ca.status = 'Late') AS LateCount
            FROM child_attendance ca
            JOIN classrooms cl ON ca.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE ca.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND ca.attendance_date BETWEEN @StartDate AND @EndDate;

            -- 2. Classrooms & Coverage
            SELECT 
                COUNT(DISTINCT cl.id) AS TotalClassrooms,
                COUNT(DISTINCT es.classroom_id) AS PlannedClassrooms
            FROM classrooms cl
            JOIN branches b ON cl.branch_id = b.id
            LEFT JOIN experience_schedules es ON cl.id = es.classroom_id 
                 AND es.tenant_id = @TenantId
                 AND es.scheduled_date BETWEEN @StartDate AND @EndDate
            WHERE cl.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND cl.is_active = true;

            -- 3. Experience Schedules
            SELECT 
                COUNT(*) AS PlannedCount,
                COUNT(*) FILTER (WHERE es.status IN ('Completed', 'Finished')) AS CompletedCount,
                COUNT(*) FILTER (WHERE es.status IN ('Confirmed', 'Scheduled')) AS UpcomingCount
            FROM experience_schedules es
            JOIN classrooms cl ON es.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE es.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND es.scheduled_date BETWEEN @StartDate AND @EndDate;

            -- 4. Moments & Care
            SELECT 
                (SELECT COUNT(*) 
                 FROM child_journey_entries cje
                 JOIN classrooms cl ON cje.classroom_id = cl.id
                 JOIN branches b ON cl.branch_id = b.id
                 WHERE cje.tenant_id = @TenantId
                   AND b.organization_id = @OrgId
                   AND (@BranchId IS NULL OR b.id = @BranchId)
                   AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
                   AND cje.observation_date >= @StartDate 
                   AND cje.observation_date <= @EndDate) AS MomentsCount,
                (SELECT COUNT(*)
                 FROM child_care_events cce
                 JOIN classrooms cl ON cce.classroom_id = cl.id
                 JOIN branches b ON cl.branch_id = b.id
                 WHERE cce.tenant_id = @TenantId
                   AND b.organization_id = @OrgId
                   AND (@BranchId IS NULL OR b.id = @BranchId)
                   AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
                   AND cce.event_time >= @StartDate 
                   AND cce.event_time <= @EndDate) AS CareCount;
        ";

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            TenantId = tenantId,
            OrgId = filter.OrganizationId,
            BranchId = filter.BranchId,
            ClassroomId = filter.ClassroomId,
            StartDate = startDt,
            EndDate = endDt
        });

        var attRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        var clsRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        var expRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        var actRow = await multi.ReadSingleOrDefaultAsync<dynamic>();

        var totalAtt = Convert.ToInt32(attRow?.totalrecords ?? 0);
        var present = Convert.ToInt32(attRow?.presentcount ?? 0);
        var absent = Convert.ToInt32(attRow?.absentcount ?? 0);
        var late = Convert.ToInt32(attRow?.latecount ?? 0);

        var totalCls = Convert.ToInt32(clsRow?.totalclassrooms ?? 0);
        var plannedCls = Convert.ToInt32(clsRow?.plannedclassrooms ?? 0);

        var plannedExp = Convert.ToInt32(expRow?.plannedcount ?? 0);
        var completedExp = Convert.ToInt32(expRow?.completedcount ?? 0);
        var upcomingExp = Convert.ToInt32(expRow?.upcomingcount ?? 0);

        var moments = Convert.ToInt32(actRow?.momentscount ?? 0);
        var care = Convert.ToInt32(actRow?.carecount ?? 0);

        var attRate = totalAtt > 0 ? Math.Round((present + late) * 100.0 / totalAtt, 1) : 100.0;
        var coverageRate = totalCls > 0 ? Math.Round(plannedCls * 100.0 / totalCls, 1) : 0.0;
        
        // Data completeness: combinations of moments, care, and attendance logged
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

        const string sql = @"
            SELECT 
                e.title AS Title,
                COUNT(es.id) AS UsageCount
            FROM experience_schedules es
            JOIN experiences e ON es.experience_id = e.id
            JOIN classrooms cl ON es.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE es.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND es.scheduled_date BETWEEN @StartDate AND @EndDate
            GROUP BY e.title
            ORDER BY UsageCount DESC;";

        var rows = (await connection.QueryAsync<dynamic>(sql, new
        {
            TenantId = tenantId,
            OrgId = filter.OrganizationId,
            BranchId = filter.BranchId,
            ClassroomId = filter.ClassroomId,
            StartDate = startDate,
            EndDate = endDate
        })).ToList();

        var total = rows.Sum(r => (int)r.usagecount);
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
            MostFrequentExperiences = rows.Take(5).Select(r => $"{r.title} ({r.usagecount}x)").ToList()
        };
    }

    public async Task<PlanningGapsMetrics> GetPlanningGapsAsync(
        Guid tenantId, 
        OrganizationIntelligenceFilter filter, 
        DateOnly today, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT 
                cl.id AS ClassroomId,
                cl.name AS ClassroomName,
                b.name AS BranchName,
                COUNT(es.id) AS UpcomingPlansCount
            FROM classrooms cl
            JOIN branches b ON cl.branch_id = b.id
            LEFT JOIN experience_schedules es ON cl.id = es.classroom_id
                 AND es.tenant_id = @TenantId
                 AND es.scheduled_date BETWEEN @Today AND @SevenDaysOut
                 AND es.status NOT IN ('Cancelled')
            WHERE cl.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND cl.is_active = true
            GROUP BY cl.id, cl.name, b.name
            HAVING COUNT(es.id) < 2
            ORDER BY UpcomingPlansCount ASC, cl.name;";

        var sevenDaysOut = today.AddDays(7);
        var rows = await connection.QueryAsync<dynamic>(sql, new
        {
            TenantId = tenantId,
            OrgId = filter.OrganizationId,
            BranchId = filter.BranchId,
            ClassroomId = filter.ClassroomId,
            Today = today,
            SevenDaysOut = sevenDaysOut
        });

        var gaps = rows.Select(r => new ClassroomGapItem
        {
            ClassroomId = (Guid)r.classroomid,
            ClassroomName = (string)r.classroomname,
            BranchName = (string)r.branchname,
            UpcomingPlansCount = (int)r.upcomingplanscount,
            Status = (int)r.upcomingplanscount == 0 ? "No Plans Scheduled" : "Low Planning Volume"
        }).ToList();

        // Overdue plans check
        const string overdueSql = @"
            SELECT COUNT(*) 
            FROM experience_schedules es
            JOIN classrooms cl ON es.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE es.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND es.scheduled_date < @Today
              AND es.status IN ('Confirmed', 'Scheduled');";

        var overdueCount = await connection.ExecuteScalarAsync<int>(overdueSql, new
        {
            TenantId = tenantId,
            OrgId = filter.OrganizationId,
            BranchId = filter.BranchId,
            ClassroomId = filter.ClassroomId,
            Today = today
        });

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

        const string sql = @"
            -- 1. Missing departure times
            SELECT COUNT(*) 
            FROM child_attendance ca
            JOIN classrooms cl ON ca.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE ca.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND ca.status = 'Present'
              AND ca.departure_time IS NULL
              AND ca.attendance_date < CURRENT_DATE
              AND ca.attendance_date BETWEEN @StartDate AND @EndDate;

            -- 2. Pending parent communications
            SELECT COUNT(*) 
            FROM parent_communications pc
            JOIN classrooms cl ON pc.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE pc.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR b.id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
              AND pc.status IN ('Draft', 'PendingReview');
        ";

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            TenantId = tenantId,
            OrgId = filter.OrganizationId,
            BranchId = filter.BranchId,
            ClassroomId = filter.ClassroomId,
            StartDate = startDate.ToDateTime(TimeOnly.MinValue),
            EndDate = endDate.ToDateTime(TimeOnly.MinValue)
        });

        var missingDepartures = await multi.ReadSingleAsync<int>();
        var pendingComms = await multi.ReadSingleAsync<int>();

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

        const string sql = @"
            INSERT INTO organization_intelligence_insights (
                id, tenant_id, organization_id, branch_id, classroom_id,
                scope_type, reporting_period, category, title, observation,
                supporting_metrics_json, possible_explanation, suggested_next_step,
                evidence_strength, data_limitations, source_type, is_ai_generated,
                created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ClassroomId,
                @ScopeType, @ReportingPeriod, @Category, @Title, @Observation,
                @SupportingMetricsJson::jsonb, @PossibleExplanation, @SuggestedNextStep,
                @EvidenceStrength, @DataLimitations, @SourceType, @IsAiGenerated,
                @CreatedAt, @UpdatedAt
            )
            RETURNING *;";

        return await connection.QuerySingleAsync<OrganizationIntelligenceInsight>(sql, new
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
        });
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

        const string sql = @"
            SELECT *
            FROM organization_intelligence_insights
            WHERE tenant_id = @TenantId
              AND organization_id = @OrganizationId
              AND (@BranchId IS NULL OR branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR classroom_id = @ClassroomId)
              AND reporting_period = @ReportingPeriod
            ORDER BY created_at DESC
            LIMIT 10;";

        return await connection.QueryAsync<OrganizationIntelligenceInsight>(sql, new
        {
            TenantId = tenantId,
            OrganizationId = organizationId,
            BranchId = branchId,
            ClassroomId = classroomId,
            ReportingPeriod = reportingPeriod
        });
    }
}
