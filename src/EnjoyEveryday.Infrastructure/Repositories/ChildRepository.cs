using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildRepository : IChildRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, first_name, last_name, date_of_birth, classroom_id, is_active, created_at, updated_at
            FROM children
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Child>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, first_name, last_name, date_of_birth, classroom_id, is_active, created_at, updated_at
            FROM children
            WHERE tenant_id = @TenantId
            ORDER BY first_name, last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Child>(sql, new { TenantId = tenantId });
    }

    public async Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, first_name, last_name, date_of_birth, classroom_id, is_active, created_at, updated_at
            FROM children
            WHERE classroom_id = @ClassroomId AND tenant_id = @TenantId
            ORDER BY first_name, last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Child>(sql, new { ClassroomId = classroomId, TenantId = tenantId });
    }

    public async Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO children (id, tenant_id, first_name, last_name, date_of_birth, classroom_id, is_active, created_at, updated_at)
            VALUES (@Id, @TenantId, @FirstName, @LastName, @DateOfBirth, @ClassroomId, @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id, first_name, last_name, date_of_birth, classroom_id, is_active, created_at, updated_at";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Child>(sql, child);
    }

    public async Task UpdateAsync(Child child, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE children
            SET first_name = @FirstName,
                last_name = @LastName,
                date_of_birth = @DateOfBirth,
                classroom_id = @ClassroomId,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, child);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM children
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
}
