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

public class StaffRosterRepository : IStaffRosterRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public StaffRosterRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    private static readonly string[] InsertStaffLeaveParams =
    [
        "Id", "TenantId", "TeacherId", "LeaveType", "StartDate", "EndDate",
        "Reason", "Status", "CoverageStatus", "SubstituteTeacherId", "ReviewedByUserId"
    ];

    public async Task<BranchBriefRecord?> GetBranchBriefAsync(Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<BranchBriefRecord>(
            "public.fn_get_branch_brief",
            new { BranchId = branchId },
            cancellationToken: ct);
    }

    public async Task<List<RatioRuleRecord>> GetStatutoryRatioRulesAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<RatioRuleRecord>(
            "public.fn_get_statutory_ratio_rules",
            new { TenantId = tenantId },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<List<RosterClassroomRecord>> GetBranchClassroomsAsync(Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<RosterClassroomRecord>(
            "public.fn_get_branch_classrooms",
            new { BranchId = branchId },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<List<ChildAttendanceSummaryRecord>> GetChildAttendanceCountsAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<ChildAttendanceSummaryRecord>(
            "public.fn_get_child_attendance_counts",
            new { TenantId = tenantId, BranchId = branchId, Date = date.Date },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<List<TeacherAttendanceSummaryRecord>> GetTeacherAttendanceAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<TeacherAttendanceSummaryRecord>(
            "public.fn_get_teacher_attendance_summary",
            new { TenantId = tenantId, BranchId = branchId, Date = date.Date },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<List<TeacherAssignmentSummaryRecord>> GetBranchTeachersAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<TeacherAssignmentSummaryRecord>(
            "public.fn_get_branch_teachers",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<List<StaffLeaveSummaryRecord>> GetActiveStaffLeavesAsync(Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<StaffLeaveSummaryRecord>(
            "public.fn_get_active_staff_leaves",
            new { TenantId = tenantId, Date = date.Date },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task InsertStaffLeaveAsync(StaffLeave leave, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "public.sp_insert_staff_leave",
            leave,
            parameterNames: InsertStaffLeaveParams,
            cancellationToken: ct);
    }

    public async Task AssignSubstituteCoverageAsync(Guid tenantId, Guid leaveId, Guid substituteTeacherId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "public.sp_assign_substitute_coverage",
            new { TenantId = tenantId, LeaveId = leaveId, SubstituteTeacherId = substituteTeacherId },
            cancellationToken: ct);
    }

    public async Task<Guid?> GetTeacherUserIdAsync(Guid tenantId, Guid teacherId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<Guid?>(
            "public.fn_get_teacher_user_id",
            new { TenantId = tenantId, TeacherId = teacherId },
            cancellationToken: ct);
    }

    public async Task DispatchTeacherToClassroomAsync(Guid tenantId, Guid classroomId, Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "public.sp_dispatch_teacher_to_classroom",
            new { TenantId = tenantId, ClassroomId = classroomId, UserId = userId },
            cancellationToken: ct);
    }

    public async Task<List<ActivityCoverageNeedRecord>> GetActivityCoverageNeedsAsync(Guid tenantId, Guid branchId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<ActivityCoverageNeedRecord>(
            "public.fn_get_coverage_needs",
            new { TenantId = tenantId, BranchId = branchId, From = from.Date, To = to.Date },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<List<CoverCandidateRecord>> GetCoverCandidatesAsync(Guid tenantId, Guid branchId, Guid eventId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<CoverCandidateRecord>(
            "public.fn_get_cover_candidates_for_event",
            new { TenantId = tenantId, BranchId = branchId, EventId = eventId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task ReassignEventOrganizerAsync(Guid tenantId, Guid branchId, Guid eventId, Guid newUserId, Guid? reassignedBy, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteScalarFunctionAsync<object>(
            "public.fn_reassign_event_organizer",
            new { TenantId = tenantId, EventId = eventId, NewUserId = newUserId, ReassignedBy = reassignedBy, BranchId = branchId },
            cancellationToken: ct);
    }
}
