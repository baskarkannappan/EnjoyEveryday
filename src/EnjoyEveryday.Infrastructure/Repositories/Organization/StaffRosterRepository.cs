using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class StaffRosterRepository : IStaffRosterRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public StaffRosterRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<BranchBriefRecord?> GetBranchBriefAsync(Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "SELECT id AS Id, name AS Name FROM branches WHERE id = @BranchId";
        return await conn.QueryFirstOrDefaultAsync<BranchBriefRecord>(new CommandDefinition(sql, new { BranchId = branchId }, cancellationToken: ct));
    }

    public async Task<List<RatioRuleRecord>> GetStatutoryRatioRulesAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT age_group AS AgeGroup, max_children_per_educator AS MaxRatio, description AS Description
            FROM classroom_statutory_ratios
            WHERE (tenant_id IS NULL OR tenant_id = @TenantId) AND is_active = TRUE";

        var results = await conn.QueryAsync<RatioRuleRecord>(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<List<RosterClassroomRecord>> GetBranchClassroomsAsync(Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id AS Id, name AS Name, COALESCE(classroom_type, age_group, 'Classroom') AS RoomType, capacity AS Capacity
            FROM classrooms
            WHERE branch_id = @BranchId
            ORDER BY name";

        var results = await conn.QueryAsync<RosterClassroomRecord>(new CommandDefinition(sql, new { BranchId = branchId }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<List<ChildAttendanceSummaryRecord>> GetChildAttendanceCountsAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT classroom_id AS ClassroomId, COUNT(*)::int AS PresentCount
            FROM child_attendance
            WHERE tenant_id = @TenantId AND branch_id = @BranchId AND attendance_date = @Date
              AND (status = 'Present' OR status = 'Late')
              AND departure_time IS NULL
            GROUP BY classroom_id";

        var results = await conn.QueryAsync<ChildAttendanceSummaryRecord>(new CommandDefinition(sql, new { TenantId = tenantId, BranchId = branchId, Date = date.Date }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<List<TeacherAttendanceSummaryRecord>> GetTeacherAttendanceAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT ta.teacher_id AS TeacherId, ta.status AS Status, ta.clock_in_time AS ClockInTime, ta.clock_out_time AS ClockOutTime,
                   t.first_name || ' ' || t.last_name AS TeacherName,
                   ct.classroom_id AS ClassroomId
            FROM teacher_attendance ta
            JOIN teachers t ON ta.teacher_id = t.id
            LEFT JOIN classroom_teachers ct ON t.user_id = ct.user_id
            WHERE ta.tenant_id = @TenantId AND ta.branch_id = @BranchId AND ta.attendance_date = @Date
              AND (ta.status = 'Present' OR ta.status = 'Late')
              AND ta.clock_out_time IS NULL";

        var results = await conn.QueryAsync<TeacherAttendanceSummaryRecord>(new CommandDefinition(sql, new { TenantId = tenantId, BranchId = branchId, Date = date.Date }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<List<TeacherAssignmentSummaryRecord>> GetBranchTeachersAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT t.id AS Id, t.first_name || ' ' || t.last_name AS TeacherName,
                   ct.classroom_id AS ClassroomId, cr.name AS ClassroomName
            FROM teachers t
            JOIN user_branch_assignments uba ON t.user_id = uba.user_id
            LEFT JOIN classroom_teachers ct ON t.user_id = ct.user_id
            LEFT JOIN classrooms cr ON ct.classroom_id = cr.id
            WHERE uba.branch_id = @BranchId AND t.tenant_id = @TenantId
            ORDER BY t.first_name";

        var results = await conn.QueryAsync<TeacherAssignmentSummaryRecord>(new CommandDefinition(sql, new { TenantId = tenantId, BranchId = branchId }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<List<StaffLeaveSummaryRecord>> GetActiveStaffLeavesAsync(Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT sl.id AS Id, sl.teacher_id AS TeacherId, sl.leave_type AS LeaveType, sl.start_date AS StartDate, sl.end_date AS EndDate, sl.reason AS Reason, sl.status AS Status, sl.coverage_status AS CoverageStatus,
                   sl.substitute_teacher_id AS SubstituteTeacherId,
                   t.first_name || ' ' || t.last_name AS TeacherName,
                   sub.first_name || ' ' || sub.last_name AS SubstituteTeacherName
            FROM staff_leaves sl
            JOIN teachers t ON sl.teacher_id = t.id
            LEFT JOIN teachers sub ON sl.substitute_teacher_id = sub.id
            WHERE sl.tenant_id = @TenantId
              AND sl.start_date <= @Date AND sl.end_date >= @Date
              AND sl.status != 'Cancelled' AND sl.status != 'Rejected'
            ORDER BY sl.start_date DESC";

        var results = await conn.QueryAsync<StaffLeaveSummaryRecord>(new CommandDefinition(sql, new { TenantId = tenantId, Date = date.Date }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task InsertStaffLeaveAsync(StaffLeave leave, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO staff_leaves (
                id, tenant_id, teacher_id, leave_type, start_date, end_date, reason, status, coverage_status, substitute_teacher_id, reviewed_by_user_id, created_at
            )
            VALUES (
                @Id, @TenantId, @TeacherId, @LeaveType, @StartDate, @EndDate, @Reason, @Status, @CoverageStatus, @SubstituteTeacherId, @ReviewedByUserId, NOW()
            )";

        await conn.ExecuteAsync(new CommandDefinition(sql, leave, cancellationToken: ct));
    }

    public async Task AssignSubstituteCoverageAsync(Guid tenantId, Guid leaveId, Guid substituteTeacherId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            UPDATE staff_leaves
            SET substitute_teacher_id = @SubstituteTeacherId,
                coverage_status = 'Covered'
            WHERE tenant_id = @TenantId AND id = @LeaveId";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, LeaveId = leaveId, SubstituteTeacherId = substituteTeacherId }, cancellationToken: ct));
    }

    public async Task<Guid?> GetTeacherUserIdAsync(Guid tenantId, Guid teacherId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "SELECT user_id FROM teachers WHERE id = @TeacherId AND tenant_id = @TenantId";
        return await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(sql, new { TeacherId = teacherId, TenantId = tenantId }, cancellationToken: ct));
    }

    public async Task DispatchTeacherToClassroomAsync(Guid tenantId, Guid classroomId, Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
            VALUES (gen_random_uuid(), @TenantId, @ClassroomId, @UserId, FALSE)
            ON CONFLICT (classroom_id, user_id) DO NOTHING";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId, ClassroomId = classroomId, UserId = userId }, cancellationToken: ct));
    }
}
