using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BranchRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, organization_id, name, location, is_active, created_at, updated_at
            FROM branches
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Branch>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<Branch>> GetByOrganizationIdAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, organization_id, name, location, is_active, created_at, updated_at
            FROM branches
            WHERE organization_id = @OrganizationId AND tenant_id = @TenantId
            ORDER BY name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Branch>(sql, new { OrganizationId = organizationId, TenantId = tenantId });
    }

    public async Task<Branch> AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active, created_at, updated_at)
            VALUES (@Id, @TenantId, @OrganizationId, @Name, @Location, @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id, organization_id, name, location, is_active, created_at, updated_at";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Branch>(sql, branch);
    }

    public async Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE branches
            SET name = @Name,
                location = @Location,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, branch);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM branches
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
}
