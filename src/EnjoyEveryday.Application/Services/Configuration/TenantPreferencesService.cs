using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class TenantPreferencesService : ITenantPreferencesService
{
    private readonly ITenantPreferencesRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantPreferencesService> _logger;

    private static readonly ConcurrentDictionary<Guid, (TenantPreferences Prefs, DateTimeOffset ExpiresAt)> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public TenantPreferencesService(
        ITenantPreferencesRepository repository,
        ITenantContext tenantContext,
        ILogger<TenantPreferencesService> logger)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TenantPreferences> GetPreferencesAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        var targetTenantId = tenantId ?? _tenantContext.TenantId;
        if (targetTenantId == Guid.Empty)
        {
            return new TenantPreferences { TenantId = Guid.Empty };
        }

        if (_cache.TryGetValue(targetTenantId, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.Prefs;
        }

        try
        {
            var dbPrefs = await _repository.GetByTenantIdAsync(targetTenantId, ct);
            var result = dbPrefs ?? new TenantPreferences { TenantId = targetTenantId };

            _cache[targetTenantId] = (result, DateTimeOffset.UtcNow.Add(CacheDuration));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tenant preferences from database for tenant {TenantId}. Using safe defaults.", targetTenantId);
            return new TenantPreferences { TenantId = targetTenantId };
        }
    }

    public async Task SavePreferencesAsync(TenantPreferences preferences, CancellationToken ct = default)
    {
        if (preferences == null) throw new ArgumentNullException(nameof(preferences));
        if (preferences.TenantId == Guid.Empty)
        {
            preferences.TenantId = _tenantContext.TenantId;
        }

        await _repository.UpsertAsync(preferences, ct);
        InvalidateCache(preferences.TenantId);
        _logger.LogInformation("Saved and evicted cache for tenant preferences of tenant {TenantId}", preferences.TenantId);
    }

    public void InvalidateCache(Guid tenantId)
    {
        if (tenantId != Guid.Empty)
        {
            _cache.TryRemove(tenantId, out _);
        }
    }
}
