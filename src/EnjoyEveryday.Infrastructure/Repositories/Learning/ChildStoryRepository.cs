using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildStoryRepository : IChildStoryRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ChildStoryRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<ChildStory>> GetByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ChildStory>(
            "public.fn_get_child_stories_by_child_id",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildInterest>> GetInterestsByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<ChildInterest>(
            "public.fn_get_child_interests_by_child_id",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<ChildStory> AddStoryAsync(ChildStory story, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_insert_child_story",
            story,
            cancellationToken: cancellationToken);

        return story;
    }

    public async Task<ChildInterest> AddInterestAsync(ChildInterest interest, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_insert_child_interest",
            interest,
            cancellationToken: cancellationToken);

        return interest;
    }

    public async Task UpdateStoryStatusAsync(Guid tenantId, Guid id, string status, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_update_child_story_status",
            new { Id = id, TenantId = tenantId, Status = status, UpdatedAt = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken);
    }
}
