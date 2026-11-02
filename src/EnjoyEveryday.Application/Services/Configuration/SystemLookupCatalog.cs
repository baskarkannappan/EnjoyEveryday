using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class SystemLookupCatalog : ISystemLookupCatalog
{
    private readonly IMasterLookupRepository _repository;
    private readonly ILogger<SystemLookupCatalog> _logger;

    private static readonly ConcurrentDictionary<string, (IReadOnlyList<MasterLookupItem> Items, DateTimeOffset ExpiresAt)> _cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(4);

    public SystemLookupCatalog(
        IMasterLookupRepository repository,
        ILogger<SystemLookupCatalog> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MasterLookupItem>> GetLookupsByCategoryAsync(string category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category)) return Array.Empty<MasterLookupItem>();

        var key = category.ToLowerInvariant();
        if (_cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.Items;
        }

        try
        {
            var items = await _repository.GetActiveLookupsAsync(category, ct);
            _cache[key] = (items, DateTimeOffset.UtcNow.Add(CacheDuration));
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load master lookups for category {Category}. Returning empty list.", category);
            return Array.Empty<MasterLookupItem>();
        }
    }

    public Task<IReadOnlyList<MasterLookupItem>> GetAllergensAsync(CancellationToken ct = default) =>
        GetLookupsByCategoryAsync("Allergen", ct);

    public Task<IReadOnlyList<MasterLookupItem>> GetDevelopmentDomainsAsync(CancellationToken ct = default) =>
        GetLookupsByCategoryAsync("DevelopmentDomain", ct);

    public Task<IReadOnlyList<MasterLookupItem>> GetEventCategoriesAsync(CancellationToken ct = default) =>
        GetLookupsByCategoryAsync("EventCategory", ct);

    public Task<IReadOnlyList<MasterLookupItem>> GetIncidentSeveritiesAsync(CancellationToken ct = default) =>
        GetLookupsByCategoryAsync("IncidentSeverity", ct);

    public void InvalidateCache()
    {
        _cache.Clear();
        _logger.LogInformation("Master lookups cache invalidated.");
    }
}
