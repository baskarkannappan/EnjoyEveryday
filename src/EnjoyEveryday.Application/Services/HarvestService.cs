using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class HarvestService
{
    private readonly IHarvestRepository _harvestRepository;
    private readonly ITenantContext _tenantContext;

    public HarvestService(IHarvestRepository harvestRepository, ITenantContext tenantContext)
    {
        _harvestRepository = harvestRepository;
        _tenantContext = tenantContext;
    }

    public async Task<HarvestMetricsDto> GetMonthlyMetricsAsync()
    {
        var tenantId = _tenantContext.TenantId;
        var metrics = await _harvestRepository.GetMonthlyMetricsAsync(tenantId);
        
        // Mocking PatternsDiscovered for now, as it's an AI-derived insight placeholder
        metrics.PatternsDiscovered = 8;
        
        return metrics;
    }

    public async Task<IEnumerable<HarvestPerformanceItemDto>> GetPerformanceItemsAsync()
    {
        var tenantId = _tenantContext.TenantId;
        return await _harvestRepository.GetPerformanceItemsAsync(tenantId);
    }
}
