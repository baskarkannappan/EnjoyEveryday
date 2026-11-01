using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Caching;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Caching;

/// <summary>
/// Thread-safe in-memory cache partitioned strictly by TenantId.
/// Backed entirely by Microsoft.Extensions.Caching.Memory without third-party dependencies.
/// </summary>
public class TenantMemoryCache : ITenantMemoryCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantMemoryCache> _logger;

    // Registry of active keys per tenant for prefix invalidation: TenantId -> Set of relative keys
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _tenantKeyRegistry = new();

    public TenantMemoryCache(
        IMemoryCache memoryCache,
        ITenantContext tenantContext,
        ILogger<TenantMemoryCache> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;
        var fullKey = BuildFullKey(tenantId, key);

        if (_memoryCache.TryGetValue(fullKey, out T? cachedValue) && cachedValue is not null)
        {
            return cachedValue;
        }

        // Track key for prefix invalidations
        RegisterKey(tenantId, key);

        var value = await factory(cancellationToken).ConfigureAwait(false);

        var options = new MemoryCacheEntryOptions();
        if (slidingExpiration.HasValue)
        {
            options.SetSlidingExpiration(slidingExpiration.Value);
        }
        else if (absoluteExpirationRelativeToNow.HasValue)
        {
            options.SetAbsoluteExpiration(absoluteExpirationRelativeToNow.Value);
        }
        else
        {
            // Default safe in-memory cache window for daycare operational metadata (5 minutes sliding)
            options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
        }

        // Register post-eviction callback to remove key from registry
        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            UnregisterKey(tenantId, key);
        });

        _memoryCache.Set(fullKey, value, options);
        return value;
    }

    public void Remove(string key)
    {
        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;
        RemoveForTenant(tenantId, key);
    }

    public void RemoveForTenant(Guid tenantId, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var fullKey = BuildFullKey(tenantId, key);
        _memoryCache.Remove(fullKey);
        UnregisterKey(tenantId, key);
        _logger.LogDebug("Cache entry invalidated: {FullKey}", fullKey);
    }

    public void RemoveByPrefix(string prefix)
    {
        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;
        RemoveByPrefixForTenant(tenantId, prefix);
    }

    public void RemoveByPrefixForTenant(Guid tenantId, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return;

        if (_tenantKeyRegistry.TryGetValue(tenantId, out var keys))
        {
            var matchingKeys = keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var key in matchingKeys)
            {
                var fullKey = BuildFullKey(tenantId, key);
                _memoryCache.Remove(fullKey);
                keys.TryRemove(key, out _);
            }
            _logger.LogDebug("Invalidated {Count} cache entries for prefix '{Prefix}' on tenant {TenantId}", matchingKeys.Count, prefix, tenantId);
        }
    }

    private static string BuildFullKey(Guid tenantId, string key) => $"ee:t:{tenantId}:{key}";

    private static void RegisterKey(Guid tenantId, string key)
    {
        var keys = _tenantKeyRegistry.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, byte>());
        keys.TryAdd(key, 0);
    }

    private static void UnregisterKey(Guid tenantId, string key)
    {
        if (_tenantKeyRegistry.TryGetValue(tenantId, out var keys))
        {
            keys.TryRemove(key, out _);
        }
    }
}
