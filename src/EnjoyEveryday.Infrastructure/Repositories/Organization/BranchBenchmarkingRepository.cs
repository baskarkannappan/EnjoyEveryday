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

public class BranchBenchmarkingRepository : IBranchBenchmarkingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BranchBenchmarkingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<List<BranchScorecardBranchRecord>> GetBranchesForBenchmarkingAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string branchesSql = @"
            SELECT id, name, COALESCE(city, 'Main Campus') AS Location
            FROM branches
            WHERE tenant_id = @tenantId AND organization_id = @organizationId
            ORDER BY name";

        var branches = (await conn.QueryAsync<BranchScorecardBranchRecord>(
            new CommandDefinition(branchesSql, new { tenantId, organizationId }, cancellationToken: ct))).ToList();

        if (!branches.Any())
        {
            const string allBranchesSql = @"
                SELECT id, name, COALESCE(city, 'Main Campus') AS Location
                FROM branches
                WHERE tenant_id = @tenantId
                ORDER BY name";

            branches = (await conn.QueryAsync<BranchScorecardBranchRecord>(
                new CommandDefinition(allBranchesSql, new { tenantId }, cancellationToken: ct))).ToList();
        }

        return branches;
    }

    public async Task<BranchRoomStatsRecord> GetRoomStatsAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string roomSql = @"
            SELECT 
                COUNT(*) AS RoomCount,
                COALESCE(SUM(capacity), 0) AS TotalCap
            FROM classrooms
            WHERE tenant_id = @tenantId AND branch_id = @BranchId";

        var stats = await conn.QuerySingleOrDefaultAsync<BranchRoomStatsRecord>(
            new CommandDefinition(roomSql, new { tenantId, BranchId = branchId }, cancellationToken: ct));

        return stats ?? new BranchRoomStatsRecord();
    }

    public async Task<int> GetEnrolledChildrenCountAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string enrolledSql = @"
            SELECT COUNT(*)
            FROM children c
            JOIN classrooms cl ON c.classroom_id = cl.id
            WHERE c.tenant_id = @tenantId AND cl.branch_id = @BranchId AND c.is_active = TRUE";

        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(enrolledSql, new { tenantId, BranchId = branchId }, cancellationToken: ct));
    }

    public async Task<BranchAttendanceStatsRecord> GetAttendanceStatsAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string attSql = @"
            SELECT 
                COUNT(*) AS TotalAttendanceRecords,
                COUNT(CASE WHEN status = 'Present' THEN 1 END) AS PresentCount
            FROM child_attendance
            WHERE tenant_id = @tenantId AND branch_id = @BranchId
              AND attendance_date >= CURRENT_DATE - INTERVAL '30 days'";

        var stats = await conn.QuerySingleOrDefaultAsync<BranchAttendanceStatsRecord>(
            new CommandDefinition(attSql, new { tenantId, BranchId = branchId }, cancellationToken: ct));

        return stats ?? new BranchAttendanceStatsRecord();
    }

    public async Task<int> GetClassroomsWithPlansCountAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string planSql = @"
            SELECT 
                COUNT(DISTINCT classroom_id) AS ClassroomsWithPlans
            FROM experience_plans
            WHERE tenant_id = @tenantId AND branch_id = @BranchId
              AND plan_date >= CURRENT_DATE - INTERVAL '7 days'";

        try
        {
            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(planSql, new { tenantId, BranchId = branchId }, cancellationToken: ct));
        }
        catch
        {
            return 0;
        }
    }

    public async Task<BranchBillingStatsRecord> GetBillingStatsAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string billSql = @"
            SELECT 
                COALESCE(SUM(total_amount), 0) AS TotalBilled,
                COALESCE(SUM(amount_paid), 0) AS TotalPaid
            FROM invoices
            WHERE tenant_id = @tenantId";

        var stats = await conn.QuerySingleOrDefaultAsync<BranchBillingStatsRecord>(
            new CommandDefinition(billSql, new { tenantId }, cancellationToken: ct));

        return stats ?? new BranchBillingStatsRecord();
    }

    public async Task<List<BranchOperationalGoal>> GetOperationalGoalsAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT 
                g.id,
                g.tenant_id AS TenantId,
                g.organization_id AS OrganizationId,
                g.branch_id AS BranchId,
                b.name AS BranchName,
                g.title AS Title,
                g.category AS Category,
                g.target_metric AS TargetMetric,
                g.current_value AS CurrentValue,
                g.target_value AS TargetValue,
                g.status AS Status,
                g.target_date AS TargetDate,
                g.owner_name AS OwnerName,
                g.created_at AS CreatedAt,
                g.updated_at AS UpdatedAt
            FROM branch_operational_goals g
            LEFT JOIN branches b ON g.branch_id = b.id
            WHERE g.tenant_id = @tenantId
              AND (@branchId IS NULL OR g.branch_id = @branchId)
            ORDER BY g.created_at DESC";

        var results = await conn.QueryAsync<BranchOperationalGoal>(
            new CommandDefinition(sql, new { tenantId, branchId }, cancellationToken: ct));

        return results.ToList();
    }

    public async Task CreateOperationalGoalAsync(BranchOperationalGoal goal, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            INSERT INTO branch_operational_goals (
                id, tenant_id, organization_id, branch_id, title, category,
                target_metric, current_value, target_value, status, target_date, owner_name
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @Title, @Category,
                @TargetMetric, @CurrentValue, @TargetValue, @Status, @TargetDate, @OwnerName
            )";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            goal.Id,
            goal.TenantId,
            goal.OrganizationId,
            goal.BranchId,
            goal.Title,
            goal.Category,
            goal.TargetMetric,
            goal.CurrentValue,
            goal.TargetValue,
            goal.Status,
            goal.TargetDate,
            goal.OwnerName
        }, cancellationToken: ct));
    }

    public async Task UpdateGoalStatusAsync(Guid tenantId, Guid goalId, string status, string? currentValue, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            UPDATE branch_operational_goals
            SET status = @status,
                current_value = COALESCE(@currentValue, current_value),
                updated_at = NOW()
            WHERE tenant_id = @tenantId AND id = @goalId";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { tenantId, goalId, status, currentValue }, cancellationToken: ct));
    }
}
