using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ClassroomRepository : IClassroomRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClassroomRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, branch_id, name, age_group, capacity, environment, is_active, created_at, updated_at
            FROM classrooms
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Classroom>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, branch_id, name, age_group, capacity, environment, is_active, created_at, updated_at
            FROM classrooms
            WHERE branch_id = @BranchId AND tenant_id = @TenantId
            ORDER BY name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Classroom>(sql, new { BranchId = branchId, TenantId = tenantId });
    }

    public async Task<Classroom> AddAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO classrooms (id, tenant_id, branch_id, name, age_group, capacity, environment, is_active, created_at, updated_at)
            VALUES (@Id, @TenantId, @BranchId, @Name, @AgeGroup, @Capacity, @Environment, @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id, branch_id, name, age_group, capacity, environment, is_active, created_at, updated_at";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Classroom>(sql, classroom);
    }

    public async Task UpdateAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE classrooms
            SET name = @Name,
                age_group = @AgeGroup,
                capacity = @Capacity,
                environment = @Environment,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, classroom);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM classrooms
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
}
