using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class DaycareOperationsBoardRepository : IDaycareOperationsBoardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DaycareOperationsBoardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<Guid?> GetDefaultOrganizationIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "SELECT id FROM organizations WHERE tenant_id = @TenantId AND is_active = true ORDER BY name LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));
    }

    public async Task<string?> GetOrganizationNameAsync(Guid tenantId, Guid orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "SELECT name FROM organizations WHERE id = @Id AND tenant_id = @TenantId";
        return await conn.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(sql, new { Id = orgId, TenantId = tenantId }, cancellationToken: ct));
    }

    public async Task<List<BoardClassroomRecord>> GetClassroomsAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                cl.id AS ClassroomId,
                cl.name AS ClassroomName,
                cl.age_group AS AgeGroup,
                cl.branch_id AS BranchId,
                b.name AS BranchName,
                cl.capacity AS Capacity,
                cl.current_enrollment AS EnrolledChildren,
                COALESCE((
                    SELECT COUNT(1) FROM child_attendance ca
                    WHERE ca.classroom_id = cl.id AND ca.attendance_date = @Date 
                      AND ca.status IN ('Present', 'Late') AND ca.tenant_id = @TenantId
                ), 0) AS PresentChildren,
                COALESCE((
                    SELECT COUNT(1) FROM children ch
                    WHERE ch.classroom_id = cl.id AND ch.is_active = true AND ch.tenant_id = @TenantId
                ), 0) AS ExpectedChildren,
                COALESCE((
                    SELECT COUNT(1) FROM teacher_attendance ta
                    WHERE ta.branch_id = cl.branch_id AND ta.attendance_date = @Date
                      AND ta.status = 'Present' AND ta.tenant_id = @TenantId
                ), 0) AS PresentStaff
            FROM classrooms cl
            JOIN branches b ON cl.branch_id = b.id
            WHERE cl.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND cl.is_active = true
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
            ORDER BY b.name, cl.name";

        var results = await conn.QueryAsync<BoardClassroomRecord>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            OrgId = orgId,
            Date = date,
            BranchId = branchId,
            ClassroomId = classroomId
        }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<BoardChildAttendanceSummaryRecord> GetChildAttendanceSummaryAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                COUNT(1) FILTER (WHERE ca.status IN ('Present', 'Late')) AS CheckedIn,
                COUNT(1) FILTER (WHERE ca.departure_time IS NOT NULL OR ca.status = 'Departed') AS CheckedOut,
                COUNT(1) FILTER (WHERE ca.status = 'Absent') AS Absent
            FROM child_attendance ca
            JOIN classrooms cl ON ca.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE ca.tenant_id = @TenantId 
              AND b.organization_id = @OrgId
              AND ca.attendance_date = @Date
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            OrgId = orgId,
            Date = date,
            BranchId = branchId,
            ClassroomId = classroomId
        }, cancellationToken: ct));

        return new BoardChildAttendanceSummaryRecord
        {
            CheckedIn = Convert.ToInt32(row?.checkedin ?? 0),
            CheckedOut = Convert.ToInt32(row?.checkedout ?? 0),
            Absent = Convert.ToInt32(row?.absent ?? 0)
        };
    }

    public async Task<BoardStaffSummaryRecord> GetStaffSummaryAsync(Guid tenantId, Guid orgId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                COUNT(1) AS ExpectedStaff,
                COUNT(1) FILTER (WHERE ta.status = 'Present') AS PresentStaff,
                COUNT(1) FILTER (WHERE ta.status = 'Absent') AS AbsentStaff
            FROM teachers t
            LEFT JOIN teacher_attendance ta ON ta.teacher_id = t.id AND ta.attendance_date = @Date AND ta.tenant_id = @TenantId
            WHERE t.tenant_id = @TenantId AND t.status != 'Inactive'
              AND EXISTS (
                  SELECT 1 FROM organization_memberships om
                  WHERE om.user_id = t.user_id AND om.organization_id = @OrgId AND om.status = 'Active'
              )";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            OrgId = orgId,
            Date = date
        }, cancellationToken: ct));

        return new BoardStaffSummaryRecord
        {
            ExpectedStaff = Convert.ToInt32(row?.expectedstaff ?? 0),
            PresentStaff = Convert.ToInt32(row?.presentstaff ?? 0),
            AbsentStaff = Convert.ToInt32(row?.absentstaff ?? 0)
        };
    }

    public async Task<List<BoardRhythmScheduleRecord>> GetExperienceSchedulesAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                es.id,
                false AS IsCelebration,
                COALESCE(e.title, 'Classroom Activity') AS Title,
                COALESCE(es.time_of_day, 'Morning') AS TimeDisplay,
                cl.name AS ScopeDisplay,
                es.status AS Status,
                es.classroom_id AS ClassroomId,
                cl.name AS ClassroomName,
                es.preparation_notes AS PreparationNotes
            FROM experience_schedules es
            JOIN classrooms cl ON es.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            LEFT JOIN experiences e ON es.experience_id = e.id
            WHERE es.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND es.scheduled_date = @Date
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
            ORDER BY es.planned_start_time NULLS LAST, es.time_of_day";

        var results = await conn.QueryAsync<BoardRhythmScheduleRecord>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            OrgId = orgId,
            Date = date,
            BranchId = branchId,
            ClassroomId = classroomId
        }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<List<BoardRhythmScheduleRecord>> GetEventsAsync(Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                ev.id,
                true AS IsCelebration,
                ev.title AS Title,
                COALESCE(TO_CHAR(ev.start_time, 'HH24:MI'), 'All Day') AS TimeDisplay,
                CASE WHEN ev.is_organization_wide THEN 'All Campus' ELSE 'Classroom Event' END AS ScopeDisplay,
                ev.status AS Status,
                ev.description AS PreparationNotes
            FROM events ev
            WHERE ev.tenant_id = @TenantId
              AND ev.event_date = @Date
            ORDER BY ev.start_time NULLS LAST";

        var results = await conn.QueryAsync<BoardRhythmScheduleRecord>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            Date = date
        }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<List<BoardTeacherChildCareRecord>> GetTeacherChildrenCareStatusAsync(Guid tenantId, Guid orgId, DateTime date, DateTime startOfDay, DateTime endOfDay, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                c.id AS ChildId,
                c.first_name || ' ' || c.last_name AS FullName,
                c.photo_url AS PhotoUrl,
                true AS IsCheckedIn,
                ca.arrival_time AS ArrivalTime,
                COALESCE((
                    SELECT COUNT(1) FROM child_care_events cce 
                    WHERE cce.child_id = c.id AND cce.tenant_id = @TenantId 
                      AND cce.event_time >= @StartOfDay AND cce.event_time <= @EndOfDay
                ), 0) AS CareEventsCount,
                (
                    SELECT cce.event_type FROM child_care_events cce
                    WHERE cce.child_id = c.id AND cce.tenant_id = @TenantId 
                      AND cce.event_time >= @StartOfDay AND cce.event_time <= @EndOfDay
                    ORDER BY cce.event_time DESC LIMIT 1
                ) AS LastRecordedCareType,
                (
                    SELECT cce.event_time FROM child_care_events cce
                    WHERE cce.child_id = c.id AND cce.tenant_id = @TenantId 
                      AND cce.event_time >= @StartOfDay AND cce.event_time <= @EndOfDay
                    ORDER BY cce.event_time DESC LIMIT 1
                ) AS LastRecordedTime
            FROM children c
            JOIN child_attendance ca ON c.id = ca.child_id AND ca.attendance_date = @Date AND ca.status IN ('Present', 'Late')
            JOIN classrooms cl ON c.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE c.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)";

        var results = await conn.QueryAsync<BoardTeacherChildCareRecord>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            OrgId = orgId,
            Date = date,
            StartOfDay = startOfDay,
            EndOfDay = endOfDay,
            BranchId = branchId,
            ClassroomId = classroomId
        }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task<int> GetPendingEnrollmentsCountAsync(Guid tenantId, Guid orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT COUNT(1) FROM children c
            LEFT JOIN classrooms cl ON c.classroom_id = cl.id
            LEFT JOIN branches b ON cl.branch_id = b.id
            WHERE c.tenant_id = @TenantId
              AND (@OrgId IS NULL OR b.organization_id = @OrgId OR c.classroom_id IS NULL)
              AND (c.enrollment_status = 'Draft' OR c.profile_completion_percentage < 100)";

        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            OrgId = orgId
        }, cancellationToken: ct));
    }

    public async Task<BoardFinancialSummaryRecord> GetFinancialSummaryAsync(Guid tenantId, Guid orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                COALESCE(SUM(total_amount - amount_paid) FILTER (WHERE status != 'Paid' AND status != 'Void'), 0) AS Outstanding,
                COUNT(1) FILTER (WHERE status != 'Paid' AND status != 'Void' AND due_date < NOW()) AS OverdueCount
            FROM invoices
            WHERE tenant_id = @TenantId
              AND organization_id = @OrgId";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(sql, new { TenantId = tenantId, OrgId = orgId }, cancellationToken: ct));

        return new BoardFinancialSummaryRecord
        {
            Outstanding = Convert.ToDecimal(row?.outstanding ?? 0),
            OverdueCount = Convert.ToInt32(row?.overduecount ?? 0)
        };
    }

    public async Task<List<PendingTeacherHandoverRecord>> GetPendingTeacherHandoversAsync(Guid tenantId, Guid orgId, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateConnectionAsync(ct);
            const string sql = @"
                SELECT 
                    th.id, th.classroom_id AS ClassroomId, cl.name AS ClassroomName,
                    th.shift_type AS ShiftType, th.outgoing_teacher_name AS OutgoingTeacherName,
                    th.is_critical AS IsCritical, th.summary_notes AS SummaryNotes,
                    th.outstanding_care_tasks AS OutstandingTasks,
                    th.created_at AS CreatedAt
                FROM teacher_handovers th
                JOIN classrooms cl ON th.classroom_id = cl.id
                JOIN branches b ON th.branch_id = b.id
                WHERE th.tenant_id = @TenantId
                  AND b.organization_id = @OrgId
                  AND th.status = 'PendingAcknowledgement'
                  AND (@BranchId IS NULL OR th.branch_id = @BranchId)
                  AND (@ClassroomId IS NULL OR th.classroom_id = @ClassroomId)";

            var results = await conn.QueryAsync<PendingTeacherHandoverRecord>(new CommandDefinition(sql, new
            {
                TenantId = tenantId,
                OrgId = orgId,
                BranchId = branchId,
                ClassroomId = classroomId
            }, cancellationToken: ct));

            return results.ToList();
        }
        catch (Exception)
        {
            return new List<PendingTeacherHandoverRecord>();
        }
    }
}
