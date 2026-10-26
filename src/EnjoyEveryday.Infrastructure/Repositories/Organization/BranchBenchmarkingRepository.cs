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

public class BranchBenchmarkingRepository : IBranchBenchmarkingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BranchBenchmarkingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    private static readonly string[] CreateGoalParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "Title", "Category",
        "TargetMetric", "CurrentValue", "TargetValue", "Status", "TargetDate", "OwnerName"
    ];

    public async Task<List<BranchScorecardBranchRecord>> GetBranchesForBenchmarkingAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var branches = (await conn.QueryFunctionAsync<BranchScorecardBranchRecord>(
            "fn_get_branches_for_benchmarking",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: ct)).ToList();

        return branches;
    }

    public async Task<BranchRoomStatsRecord> GetRoomStatsAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var stats = await conn.QuerySingleFunctionAsync<BranchRoomStatsRecord>(
            "fn_get_branch_room_stats",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);

        return stats ?? new BranchRoomStatsRecord();
    }

    public async Task<int> GetEnrolledChildrenCountAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<int>(
            "fn_get_branch_enrolled_children_count",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);
    }

    public async Task<BranchAttendanceStatsRecord> GetAttendanceStatsAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var stats = await conn.QuerySingleFunctionAsync<BranchAttendanceStatsRecord>(
            "fn_get_branch_attendance_stats",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);

        return stats ?? new BranchAttendanceStatsRecord();
    }

    public async Task<int> GetClassroomsWithPlansCountAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<int>(
            "fn_get_branch_classrooms_with_plans_count",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);
    }

    public async Task<BranchBillingStatsRecord> GetBillingStatsAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var stats = await conn.QuerySingleFunctionAsync<BranchBillingStatsRecord>(
            "fn_get_branch_billing_stats",
            new { TenantId = tenantId },
            cancellationToken: ct);

        return stats ?? new BranchBillingStatsRecord();
    }

    public async Task<List<BranchOperationalGoal>> GetOperationalGoalsAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<BranchOperationalGoal>(
            "fn_get_branch_operational_goals",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task CreateOperationalGoalAsync(BranchOperationalGoal goal, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_branch_operational_goal",
            goal,
            parameterNames: CreateGoalParams,
            cancellationToken: ct);
    }

    public async Task UpdateGoalStatusAsync(Guid tenantId, Guid goalId, string status, string? currentValue, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_branch_operational_goal_status",
            new { TenantId = tenantId, GoalId = goalId, Status = status, CurrentValue = currentValue },
            cancellationToken: ct);
    }
}
