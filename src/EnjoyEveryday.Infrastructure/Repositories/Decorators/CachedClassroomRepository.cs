using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Caching;

namespace EnjoyEveryday.Infrastructure.Repositories.Decorators;

/// <summary>
/// Thread-safe in-memory caching decorator for IClassroomRepository.
/// Eliminates redundant database roundtrips for classroom queries in dashboards and roster dropdowns.
/// </summary>
public class CachedClassroomRepository : IClassroomRepository
{
    private readonly IClassroomRepository _inner;
    private readonly ITenantMemoryCache _cache;

    public CachedClassroomRepository(IClassroomRepository inner, ITenantMemoryCache cache)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var key = $"classrooms:{id}";
        return await _cache.GetOrCreateAsync(
            key,
            ct => _inner.GetByIdAsync(tenantId, id, ct),
            slidingExpiration: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var key = $"classrooms:branch:{branchId}";
        return await _cache.GetOrCreateAsync(
            key,
            ct => _inner.GetByBranchIdAsync(tenantId, branchId, ct),
            slidingExpiration: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var key = "classrooms:all";
        return await _cache.GetOrCreateAsync(
            key,
            ct => _inner.GetAllAsync(tenantId, ct),
            slidingExpiration: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        var key = $"classrooms:all:org:{organizationId?.ToString() ?? "none"}";
        return await _cache.GetOrCreateAsync(
            key,
            ct => _inner.GetAllAsync(tenantId, organizationId, ct),
            slidingExpiration: TimeSpan.FromMinutes(10),
            cancellationToken: cancellationToken);
    }

    public async Task<Classroom> AddAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddAsync(classroom, cancellationToken);
        _cache.RemoveByPrefix("classrooms");
        return result;
    }

    public async Task UpdateAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateAsync(classroom, cancellationToken);
        _cache.RemoveByPrefix("classrooms");
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(tenantId, id, cancellationToken);
        _cache.RemoveByPrefix("classrooms");
    }
}
