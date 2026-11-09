using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Caching;

namespace EnjoyEveryday.Infrastructure.Repositories.Decorators;

/// <summary>
/// Thread-safe in-memory caching decorator for IBranchRepository.
/// Caches reads and automatically invalidates on mutations without affecting core repository logic.
/// </summary>
public class CachedBranchRepository : IBranchRepository
{
    private readonly IBranchRepository _inner;
    private readonly ITenantMemoryCache _cache;

    public CachedBranchRepository(IBranchRepository inner, ITenantMemoryCache cache)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var key = $"branches:{id}";
        return await _cache.GetOrCreateAsync(
            key,
            ct => _inner.GetByIdAsync(tenantId, id, ct),
            slidingExpiration: TimeSpan.FromSeconds(3),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Branch>> GetByOrganizationIdAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var key = $"branches:org:{organizationId}";
        return await _cache.GetOrCreateAsync(
            key,
            ct => _inner.GetByOrganizationIdAsync(tenantId, organizationId, ct),
            slidingExpiration: TimeSpan.FromSeconds(3),
            cancellationToken: cancellationToken);
    }

    public async Task<Branch> AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddAsync(branch, cancellationToken);
        _cache.RemoveByPrefix("branches");
        return result;
    }

    public async Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateAsync(branch, cancellationToken);
        _cache.RemoveByPrefix("branches");
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(tenantId, id, cancellationToken);
        _cache.RemoveByPrefix("branches");
    }
}
