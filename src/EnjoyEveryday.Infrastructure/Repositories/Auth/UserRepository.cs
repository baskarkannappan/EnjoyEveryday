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
            SELECT id as Id, tenant_id as TenantId, email as Email, first_name as FirstName, last_name as LastName, display_name as DisplayName, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
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
            WHERE u.tenant_id = @TenantId
              AND (EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON ur.role_id = r.id
                           WHERE ur.user_id = u.id AND r.name = @RoleName)
                   OR EXISTS (SELECT 1 FROM user_organization_roles uor JOIN roles r ON r.id = uor.role_id
                              WHERE uor.user_id = u.id AND uor.tenant_id = @TenantId AND r.name = ANY(@Names)))
            ORDER BY u.first_name, u.last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<User>(sql, new { TenantId = tenantId, RoleName = roleName, Names = EnjoyEveryday.Shared.Authorization.RoleTemplates.EquivalentNames(roleName) });
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
                email = @Email,
                normalized_email = @NormalizedEmail,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { 
            user.FirstName, 
            user.LastName, 
            user.DisplayName, 
            user.Email, 
            NormalizedEmail = user.Email.ToUpperInvariant(), 
            user.IsActive, 
            user.UpdatedAt, 
            user.Id, 
            user.TenantId 
        });
    }

    public async Task AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        // Global user_roles are for PLATFORM roles only; any other name inserts nothing.
        const string sql = @"
            INSERT INTO user_roles (user_id, role_id)
            SELECT @UserId, id
            FROM roles
            WHERE name = @RoleName AND tenant_id IS NULL AND scope_type = 'Platform'
            ON CONFLICT DO NOTHING";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { UserId = userId, RoleName = roleName });
    }

    public async Task AssignOrganizationRoleAsync(Guid tenantId, Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        // Every statement is bound to the tenant; the user, the organization and the role must all belong to it.
        const string sql = @"
            INSERT INTO organization_memberships (user_id, organization_id, tenant_id)
            SELECT u.id, o.id, o.tenant_id
            FROM users u JOIN organizations o ON o.tenant_id = u.tenant_id
            WHERE u.id = @UserId AND u.tenant_id = @TenantId AND o.id = @OrganizationId
            ON CONFLICT DO NOTHING;

            INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
            SELECT @UserId, r.id, @OrganizationId, @TenantId
            FROM roles r
            WHERE r.tenant_id = @TenantId AND r.name = @RoleName AND r.scope_type = 'Organization'
              AND EXISTS (SELECT 1 FROM organization_memberships m
                          WHERE m.user_id = @UserId AND m.organization_id = @OrganizationId AND m.tenant_id = @TenantId)
            ON CONFLICT DO NOTHING";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId, RoleName = roleName });
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

