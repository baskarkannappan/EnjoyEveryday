using System.Text.Json;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceRepository : IExperienceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExperienceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Experience?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, title, description, status, dna_payload, created_by_user_id, created_at, updated_at
            FROM experiences
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Experience>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, title, description, status, dna_payload, created_by_user_id, created_at, updated_at
            FROM experiences
            WHERE tenant_id = @TenantId
            ORDER BY created_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Experience>(sql, new { TenantId = tenantId });
    }

    public async Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO experiences (id, tenant_id, title, description, status, dna_payload, created_by_user_id, created_at, updated_at)
            VALUES (@Id, @TenantId, @Title, @Description, @Status, @DnaPayload::jsonb, @CreatedByUserId, @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id, title, description, status, dna_payload, created_by_user_id, created_at, updated_at";

        const string versionSql = @"
            INSERT INTO experience_versions (id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at)
            VALUES (@VersionId, @ExperienceId, 1, @Title, @Description, @DnaPayload::jsonb, 'Initial Creation', @ModifiedByUserId, @CreatedAt)";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        var inserted = await connection.QuerySingleAsync<Experience>(sql, experience);
        
        await connection.ExecuteAsync(versionSql, new 
        {
            VersionId = Guid.NewGuid(),
            ExperienceId = inserted.Id,
            inserted.Title,
            inserted.Description,
            inserted.DnaPayload,
            ModifiedByUserId = inserted.CreatedByUserId,
            inserted.CreatedAt
        });

        return inserted;
    }

    public async Task UpdateAsync(Experience experience, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        // Get latest version number
        var latestVersion = await connection.QuerySingleOrDefaultAsync<int?>(
            "SELECT MAX(version_number) FROM experience_versions WHERE experience_id = @ExperienceId", 
            new { ExperienceId = experience.Id }) ?? 0;
            
        var nextVersion = latestVersion + 1;

        const string sql = @"
            UPDATE experiences
            SET title = @Title,
                description = @Description,
                status = @Status,
                dna_payload = @DnaPayload::jsonb,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        await connection.ExecuteAsync(sql, experience);

        const string versionSql = @"
            INSERT INTO experience_versions (id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at)
            VALUES (@VersionId, @ExperienceId, @VersionNumber, @Title, @Description, @DnaPayload::jsonb, @ChangeReason, @ModifiedByUserId, @CreatedAt)";
            
        await connection.ExecuteAsync(versionSql, new 
        {
            VersionId = Guid.NewGuid(),
            ExperienceId = experience.Id,
            VersionNumber = nextVersion,
            experience.Title,
            experience.Description,
            experience.DnaPayload,
            ChangeReason = changeReason,
            ModifiedByUserId = modifiedByUserId,
            CreatedAt = experience.UpdatedAt
        });
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM experiences
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
    
    public async Task<IEnumerable<ExperienceVersion>> GetVersionsAsync(Guid experienceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at
            FROM experience_versions
            WHERE experience_id = @ExperienceId
            ORDER BY version_number DESC";
            
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<ExperienceVersion>(sql, new { ExperienceId = experienceId });
    }
}
