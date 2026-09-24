using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, email, first_name, last_name, display_name, is_active, created_at, updated_at
            FROM users
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<User>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, email, first_name, last_name, display_name, is_active, created_at, updated_at
            FROM users
            WHERE tenant_id = @TenantId
            ORDER BY first_name, last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<User>(sql, new { TenantId = tenantId });
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(Guid tenantId, string roleName, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
            FROM users u
            JOIN user_roles ur ON u.id = ur.user_id
            JOIN roles r ON ur.role_id = r.id
            WHERE u.tenant_id = @TenantId AND r.name = @RoleName
            ORDER BY u.first_name, u.last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<User>(sql, new { TenantId = tenantId, RoleName = roleName });
    }

    public async Task<User> AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO users (id, tenant_id, email, normalized_email, password_hash, first_name, last_name, display_name, is_active, created_at, updated_at)
            VALUES (@Id, @TenantId, @Email, @NormalizedEmail, @PasswordHash, @FirstName, @LastName, @DisplayName, @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING id, tenant_id, email, first_name, last_name, display_name, is_active, created_at, updated_at";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<User>(sql, new
        {
            user.Id,
            user.TenantId,
            user.Email,
            NormalizedEmail = user.Email.ToUpperInvariant(),
            PasswordHash = passwordHash,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE users
            SET first_name = @FirstName,
                last_name = @LastName,
                display_name = @DisplayName,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, user);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM users
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId });
    }
}
