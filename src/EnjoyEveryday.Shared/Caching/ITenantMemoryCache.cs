using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Shared.Caching;

/// <summary>
/// Tenant-aware in-memory caching contract.
/// Ensures that cached data is strictly partitioned by tenant ID without risking cross-tenant data leaks.
/// </summary>
public interface ITenantMemoryCache
{
    /// <summary>
    /// Retrieves or computes a cached value for the current tenant.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly removes an item from the current tenant's cache.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Explicitly removes an item for an explicitly specified tenant.
    /// </summary>
    void RemoveForTenant(Guid tenantId, string key);

    /// <summary>
    /// Removes all cached items under a specific prefix for the current tenant.
    /// (e.g. prefix = "classrooms" invalidates all classroom cache variants for that tenant).
    /// </summary>
    void RemoveByPrefix(string prefix);

    /// <summary>
    /// Removes all cached items under a specific prefix for a specified tenant.
    /// </summary>
    void RemoveByPrefixForTenant(Guid tenantId, string prefix);
}
