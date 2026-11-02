using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface ITenantPreferencesService
{
    Task<TenantPreferences> GetPreferencesAsync(Guid? tenantId = null, CancellationToken ct = default);
    Task SavePreferencesAsync(TenantPreferences preferences, CancellationToken ct = default);
    void InvalidateCache(Guid tenantId);
}
