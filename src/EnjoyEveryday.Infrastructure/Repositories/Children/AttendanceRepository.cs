using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AttendanceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] RecordChildAttendanceParams =
    [
        "Id", "TenantId", "BranchId", "ClassroomId", "ChildId", "AttendanceDate", "Status",
        "ArrivalTime", "DepartureTime", "Notes", "RecordedByUserId",
        "PickupPersonName", "PickupPersonRelationship", "PickupVerificationMethod",
        "PickupNotes", "AuthorizedPickupId", "DepartureVerifiedByUserId"
    ];

    private static readonly string[] RecordTeacherAttendanceParams =
    [
        "Id", "TenantId", "BranchId", "TeacherId", "AttendanceDate", "Status",
        "ClockInTime", "ClockOutTime", "Notes", "RecordedByUserId"
    ];

    public async Task<IEnumerable<ChildAttendance>> GetChildAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ChildAttendance>(
            "fn_get_child_attendance_by_date",
            new { TenantId = tenantId, BranchId = branchId, Date = DateOnly.FromDateTime(date) },
            parameterNames: new[] { "TenantId", "BranchId", "Date::date" },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<TeacherAttendance>> GetTeacherAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<TeacherAttendance>(
            "fn_get_teacher_attendance_by_date",
            new { TenantId = tenantId, BranchId = branchId, Date = DateOnly.FromDateTime(date) },
            parameterNames: new[] { "TenantId", "BranchId", "Date::date" },
            cancellationToken: cancellationToken);
    }

    public async Task<ChildAttendance> RecordChildAttendanceAsync(ChildAttendance attendance, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_record_child_attendance",
            attendance,
            parameterNames: RecordChildAttendanceParams,
            cancellationToken: cancellationToken);

        return attendance;
    }

    public async Task<TeacherAttendance> RecordTeacherAttendanceAsync(TeacherAttendance attendance, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QuerySingleFunctionAsync<TeacherAttendance>(
            "fn_record_teacher_attendance",
            attendance,
            parameterNames: RecordTeacherAttendanceParams,
            cancellationToken: cancellationToken);

        return result ?? attendance;
    }
    
    public async Task<AttendanceSummary> GetOrganizationSummaryAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QuerySingleFunctionAsync<AttendanceSummary>(
            "fn_get_attendance_summary",
            new { TenantId = tenantId, Date = DateOnly.FromDateTime(date) },
            parameterNames: new[] { "TenantId", "Date::date" },
            cancellationToken: cancellationToken);

        return result ?? new AttendanceSummary();
    }

    public async Task<IEnumerable<Guid>> GetPresentChildIdsByClassroomAsync(Guid tenantId, Guid classroomId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Guid>(
            "fn_get_present_child_ids_by_classroom",
            new { TenantId = tenantId, ClassroomId = classroomId, Date = DateOnly.FromDateTime(date) },
            parameterNames: new[] { "TenantId", "ClassroomId", "Date::date" },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<EnjoyEveryday.Domain.Entities.Attendance.AttendanceAttentionAlertDto>> GetAttendanceNeedsAttentionAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<EnjoyEveryday.Domain.Entities.Attendance.AttendanceAttentionAlertDto>(
            "fn_get_attendance_needs_attention",
            new { TenantId = tenantId, Date = DateOnly.FromDateTime(date) },
            parameterNames: new[] { "TenantId", "Date::date" },
            cancellationToken: cancellationToken);
    }

    public async Task<EnjoyEveryday.Domain.Entities.Attendance.BranchLocationContextDto?> GetBranchLocationContextAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleProcedureAsync<EnjoyEveryday.Domain.Entities.Attendance.BranchLocationContextDto>(
            "public.sp_get_branch_location_context",
            new { p_tenant_id = tenantId, p_branch_id = branchId },
            parameterNames: new[] { "p_tenant_id", "p_branch_id" },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<EnjoyEveryday.Domain.Entities.Attendance.BranchLocationContextDto>> GetUserAccessibleBranchesAsync(Guid tenantId, Guid userId, bool isOrgAdmin, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryProcedureAsync<EnjoyEveryday.Domain.Entities.Attendance.BranchLocationContextDto>(
            "public.sp_get_user_accessible_branches",
            new { p_tenant_id = tenantId, p_user_id = userId, p_is_org_admin = isOrgAdmin },
            parameterNames: new[] { "p_tenant_id", "p_user_id", "p_is_org_admin" },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<EnjoyEveryday.Domain.Entities.Attendance.MonthlyChildAttendanceRecordDto>> GetMonthlyChildAttendanceAsync(Guid tenantId, Guid branchId, int year, int month, Guid? classroomId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryProcedureAsync<EnjoyEveryday.Domain.Entities.Attendance.MonthlyChildAttendanceRecordDto>(
            "public.sp_get_monthly_child_attendance",
            new { p_tenant_id = tenantId, p_branch_id = branchId, p_year = year, p_month = month, p_classroom_id = classroomId },
            parameterNames: new[] { "p_tenant_id", "p_branch_id", "p_year", "p_month", "p_classroom_id" },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<EnjoyEveryday.Domain.Entities.Attendance.MonthlyTeacherAttendanceRecordDto>> GetMonthlyTeacherAttendanceAsync(Guid tenantId, Guid branchId, int year, int month, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryProcedureAsync<EnjoyEveryday.Domain.Entities.Attendance.MonthlyTeacherAttendanceRecordDto>(
            "public.sp_get_monthly_teacher_attendance",
            new { p_tenant_id = tenantId, p_branch_id = branchId, p_year = year, p_month = month },
            parameterNames: new[] { "p_tenant_id", "p_branch_id", "p_year", "p_month" },
            cancellationToken: cancellationToken);
    }
}