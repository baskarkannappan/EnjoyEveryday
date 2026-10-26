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
}