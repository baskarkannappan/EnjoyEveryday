using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class DashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ITenantContext _tenantContext;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        ITenantContext tenantContext)
    {
        _dashboardRepository = dashboardRepository;
        _tenantContext = tenantContext;
    }

    public async Task<OrganizationMetrics> GetOrganizationMetricsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetOrganizationMetricsAsync(_tenantContext.TenantId, organizationId, cancellationToken);
    }

    public async Task<IEnumerable<BranchMetrics>> GetBranchMetricsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository.GetBranchMetricsAsync(_tenantContext.TenantId, organizationId, cancellationToken);
    }
}
