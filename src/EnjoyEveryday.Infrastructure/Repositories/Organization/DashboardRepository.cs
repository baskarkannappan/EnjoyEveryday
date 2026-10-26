using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OrganizationMetrics> GetOrganizationMetricsAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var org = await connection.QuerySingleFunctionAsync<OrganizationMetrics>(
            "public.fn_get_dashboard_organization_metrics",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: cancellationToken);
        return org ?? new OrganizationMetrics();
    }

    public async Task<IEnumerable<BranchMetrics>> GetBranchMetricsAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var branches = (await connection.QueryFunctionAsync<BranchMetrics>(
            "public.fn_get_dashboard_branch_metrics",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: cancellationToken)).ToList();

        foreach (var branch in branches)
        {
            branch.ExperienceRhythm = branch.Experiences > 0 ? Math.Min(100, branch.Experiences * 10) : 0;
            branch.Teachers = branch.Classrooms * 2;
            branch.AttentionStatus = branch.Experiences < 5 ? "Needs support" : (branch.Experiences < 10 ? "Quiet this week" : "Healthy rhythm");
            branch.RecentInsight = branch.Experiences < 5 ? "Very few experiences scheduled." : "Activity is normal.";
        }

        return branches;
    }
}
