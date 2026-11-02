using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface ITenantPreferencesRepository
{
    Task<TenantPreferences?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task UpsertAsync(TenantPreferences preferences, CancellationToken ct = default);
}
