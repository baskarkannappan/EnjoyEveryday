using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
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
        return await conn.ExecuteScalarFunctionAsync<Guid?>(
            "fn_get_board_default_organization_id",
            new { TenantId = tenantId },
            cancellationToken: ct);
    }

    public async Task<string?> GetOrganizationNameAsync(Guid tenantId, Guid orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<string?>(
            "fn_get_board_organization_name",
            new { TenantId = tenantId, Id = orgId },
            cancellationToken: ct);
    }

    public async Task<List<BoardClassroomRecord>> GetClassroomsAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<BoardClassroomRecord>(
            "fn_get_board_classrooms",
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                Date = date.Date,
                BranchId = branchId,
                ClassroomId = classroomId
            },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<BoardChildAttendanceSummaryRecord> GetChildAttendanceSummaryAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<dynamic>(
            "fn_get_board_child_attendance_summary",
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                Date = date.Date,
                BranchId = branchId,
                ClassroomId = classroomId
            },
            cancellationToken: ct);

        return new BoardChildAttendanceSummaryRecord
        {
            CheckedIn = Convert.ToInt32(row?.checked_in ?? 0),
            CheckedOut = Convert.ToInt32(row?.checked_out ?? 0),
            Absent = Convert.ToInt32(row?.absent ?? 0)
        };
    }

    public async Task<BoardStaffSummaryRecord> GetStaffSummaryAsync(Guid tenantId, Guid orgId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<dynamic>(
            "fn_get_board_staff_summary",
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                Date = date.Date
            },
            cancellationToken: ct);

        return new BoardStaffSummaryRecord
        {
            ExpectedStaff = Convert.ToInt32(row?.expected_staff ?? 0),
            PresentStaff = Convert.ToInt32(row?.present_staff ?? 0),
            AbsentStaff = Convert.ToInt32(row?.absent_staff ?? 0)
        };
    }

    public async Task<List<BoardRhythmScheduleRecord>> GetExperienceSchedulesAsync(Guid tenantId, Guid orgId, DateTime date, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<BoardRhythmScheduleRecord>(
            "fn_get_board_experience_schedules",
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                Date = date.Date,
                BranchId = branchId,
                ClassroomId = classroomId
            },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<List<BoardRhythmScheduleRecord>> GetEventsAsync(Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<BoardRhythmScheduleRecord>(
            "fn_get_board_events",
            new
            {
                TenantId = tenantId,
                Date = date.Date
            },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<List<BoardTeacherChildCareRecord>> GetTeacherChildrenCareStatusAsync(Guid tenantId, Guid orgId, DateTime date, DateTime startOfDay, DateTime endOfDay, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<BoardTeacherChildCareRecord>(
            "fn_get_board_teacher_children_care_status",
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                Date = date.Date,
                StartOfDay = startOfDay,
                EndOfDay = endOfDay,
                BranchId = branchId,
                ClassroomId = classroomId
            },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<int> GetPendingEnrollmentsCountAsync(Guid tenantId, Guid orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<int>(
            "fn_get_board_pending_enrollments_count",
            new
            {
                TenantId = tenantId,
                OrgId = orgId
            },
            cancellationToken: ct);
    }

    public async Task<BoardFinancialSummaryRecord> GetFinancialSummaryAsync(Guid tenantId, Guid orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<dynamic>(
            "fn_get_board_financial_summary",
            new { TenantId = tenantId, OrgId = orgId },
            cancellationToken: ct);

        return new BoardFinancialSummaryRecord
        {
            Outstanding = Convert.ToDecimal(row?.outstanding ?? 0),
            OverdueCount = Convert.ToInt32(row?.overdue_count ?? 0)
        };
    }

    public async Task<List<PendingTeacherHandoverRecord>> GetPendingTeacherHandoversAsync(Guid tenantId, Guid orgId, Guid? branchId, Guid? classroomId, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateConnectionAsync(ct);
            var results = await conn.QueryFunctionAsync<PendingTeacherHandoverRecord>(
                "fn_get_board_pending_teacher_handovers",
                new
                {
                    TenantId = tenantId,
                    OrgId = orgId,
                    BranchId = branchId,
                    ClassroomId = classroomId
                },
                cancellationToken: ct);

            return results.ToList();
        }
        catch (Exception)
        {
            return new List<PendingTeacherHandoverRecord>();
        }
    }
}
