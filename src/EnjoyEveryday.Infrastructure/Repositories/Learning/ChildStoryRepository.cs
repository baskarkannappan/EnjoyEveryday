using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
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
        var sql = @"
            SELECT id, tenant_id as TenantId, child_id as ChildId, teacher_id as TeacherId,
                   experience_schedule_id as ExperienceScheduleId, content as Content,
                   status as Status, created_at as CreatedAt, updated_at as UpdatedAt
            FROM child_stories
            WHERE child_id = @ChildId AND tenant_id = @TenantId
            ORDER BY created_at DESC";
        return await connection.QueryAsync<ChildStory>(sql, new { ChildId = childId, TenantId = tenantId });
    }

    public async Task<IEnumerable<ChildInterest>> GetInterestsByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT id, tenant_id as TenantId, child_id as ChildId, name as Name, created_at as CreatedAt
            FROM child_interests
            WHERE child_id = @ChildId AND tenant_id = @TenantId
            ORDER BY created_at DESC";
        return await connection.QueryAsync<ChildInterest>(sql, new { ChildId = childId, TenantId = tenantId });
    }

    public async Task<ChildStory> AddStoryAsync(ChildStory story, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            INSERT INTO child_stories (id, tenant_id, child_id, teacher_id, experience_schedule_id, content, status, created_at, updated_at)
            VALUES (@Id, @TenantId, @ChildId, @TeacherId, @ExperienceScheduleId, @Content, @Status, @CreatedAt, @UpdatedAt)";
        
        await connection.ExecuteAsync(sql, story);
        return story;
    }

    public async Task<ChildInterest> AddInterestAsync(ChildInterest interest, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            INSERT INTO child_interests (id, tenant_id, child_id, name, created_at)
            VALUES (@Id, @TenantId, @ChildId, @Name, @CreatedAt)";
        
        await connection.ExecuteAsync(sql, interest);
        return interest;
    }

    public async Task UpdateStoryStatusAsync(Guid tenantId, Guid id, string status, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            UPDATE child_stories
            SET status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";
        
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId, Status = status, UpdatedAt = DateTimeOffset.UtcNow });
    }
}
