using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IDashboardRepository
{
    Task<OrganizationMetrics> GetOrganizationMetricsAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BranchMetrics>> GetBranchMetricsAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);
}
