using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class DailyOperationsRepository : IDailyOperationsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DailyOperationsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BranchDailyStatusDto?> GetBranchDailyStatusAsync(Guid tenantId, Guid branchId, DateOnly date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                branch_id AS BranchId,
                operation_date AS OperationDate,
                status AS Status,
                opened_at AS OpenedAt,
                opened_by_name AS OpenedByName,
                closed_at AS ClosedAt,
                closed_by_name AS ClosedByName,
                total_classrooms AS TotalClassrooms,
                open_classrooms AS OpenClassrooms,
                closed_classrooms AS ClosedClassrooms,
                pending_children_count AS PendingChildrenCount
            FROM public.fn_get_branch_daily_status(@TenantId, @BranchId, @Date::date);";

        return await conn.QueryFirstOrDefaultAsync<BranchDailyStatusDto>(sql, new
        {
            TenantId = tenantId,
            BranchId = branchId,
            Date = date.ToDateTime(TimeOnly.MinValue)
        });
    }

    public async Task<ClassroomDailyStatusDto?> GetClassroomDailyStatusAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT 
                classroom_id AS ClassroomId,
                branch_id AS BranchId,
                operation_date AS OperationDate,
                status AS Status,
                opened_at AS OpenedAt,
                opened_by_name AS OpenedByName,
                closed_at AS ClosedAt,
                closed_by_name AS ClosedByName,
                branch_status AS BranchStatus,
                active_children_count AS ActiveChildrenCount,
                active_activities_count AS ActiveActivitiesCount
            FROM public.fn_get_classroom_daily_status(@TenantId, @ClassroomId, @Date::date);";

        return await conn.QueryFirstOrDefaultAsync<ClassroomDailyStatusDto>(sql, new
        {
            TenantId = tenantId,
            ClassroomId = classroomId,
            Date = date.ToDateTime(TimeOnly.MinValue)
        });
    }

    public async Task<DailyOperationExecutionResult> OpenBranchDayAsync(Guid tenantId, Guid branchId, DateOnly date, Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"CALL public.sp_open_branch_day(@TenantId, @BranchId, @Date::date, @UserId, NULL, NULL);";
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            TenantId = tenantId,
            BranchId = branchId,
            Date = date.ToDateTime(TimeOnly.MinValue),
            UserId = userId
        });

        return new DailyOperationExecutionResult
        {
            Success = (bool)(row?.p_success ?? false),
            Message = (string)(row?.p_message ?? "Failed to open branch.")
        };
    }

    public async Task<DailyOperationExecutionResult> OpenClassroomDayAsync(Guid tenantId, Guid classroomId, DateOnly date, Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"CALL public.sp_open_classroom_day(@TenantId, @ClassroomId, @Date::date, @UserId, NULL, NULL);";
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            TenantId = tenantId,
            ClassroomId = classroomId,
            Date = date.ToDateTime(TimeOnly.MinValue),
            UserId = userId
        });

        return new DailyOperationExecutionResult
        {
            Success = (bool)(row?.p_success ?? false),
            Message = (string)(row?.p_message ?? "Failed to open classroom.")
        };
    }

    public async Task<DailyOperationExecutionResult> CloseClassroomDayAsync(Guid tenantId, Guid classroomId, DateOnly date, Guid userId, TimeSpan checkoutTime, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"CALL public.sp_close_classroom_day(@TenantId, @ClassroomId, @Date::date, @UserId, @CheckoutTime::time, NULL, NULL, NULL, NULL);";
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            TenantId = tenantId,
            ClassroomId = classroomId,
            Date = date.ToDateTime(TimeOnly.MinValue),
            UserId = userId,
            CheckoutTime = checkoutTime
        });

        return new DailyOperationExecutionResult
        {
            Success = (bool)(row?.p_success ?? false),
            Message = (string)(row?.p_message ?? "Failed to close classroom."),
            AffectedChildrenCount = (int)(row?.p_children_checked_out ?? 0),
            AffectedActivitiesCount = (int)(row?.p_activities_closed ?? 0)
        };
    }

    public async Task<DailyOperationExecutionResult> CloseBranchDayAsync(Guid tenantId, Guid branchId, DateOnly date, Guid userId, TimeSpan checkoutTime, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"CALL public.sp_close_branch_day(@TenantId, @BranchId, @Date::date, @UserId, @CheckoutTime::time, NULL, NULL, NULL, NULL);";
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            TenantId = tenantId,
            BranchId = branchId,
            Date = date.ToDateTime(TimeOnly.MinValue),
            UserId = userId,
            CheckoutTime = checkoutTime
        });

        return new DailyOperationExecutionResult
        {
            Success = (bool)(row?.p_success ?? false),
            Message = (string)(row?.p_message ?? "Failed to close branch."),
            OpenClassroomsCount = (int)(row?.p_open_classrooms_count ?? 0),
            AffectedTeachersCount = (int)(row?.p_teachers_clocked_out ?? 0)
        };
    }
}

