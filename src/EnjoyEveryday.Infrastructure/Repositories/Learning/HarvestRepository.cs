using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class HarvestRepository : IHarvestRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public HarvestRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<HarvestMetricsDto> GetMonthlyMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var metrics = await connection.QuerySingleFunctionAsync<HarvestMetricsDto>(
            "public.fn_get_harvest_monthly_metrics",
            new { TenantId = tenantId },
            cancellationToken: cancellationToken);

        return metrics ?? new HarvestMetricsDto();
    }

    public async Task<IEnumerable<HarvestPerformanceItemDto>> GetPerformanceItemsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<HarvestPerformanceItemDto>(
            "public.fn_get_harvest_performance_items",
            new { TenantId = tenantId },
            cancellationToken: cancellationToken);
    }
}
