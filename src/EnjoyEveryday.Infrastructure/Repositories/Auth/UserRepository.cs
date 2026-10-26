using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
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
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<User>(
            "fn_get_user_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<User>(
            "fn_get_all_users",
            new { TenantId = tenantId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(Guid tenantId, string roleName, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<User>(
            "fn_get_users_by_role",
            new
            {
                TenantId = tenantId,
                RoleName = roleName,
                Names = EnjoyEveryday.Shared.Authorization.RoleTemplates.EquivalentNames(roleName)
            },
            cancellationToken: cancellationToken);
    }

    public async Task<User> AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleFunctionAsync<User>(
            "fn_create_user",
            new
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
            },
            cancellationToken: cancellationToken);

        return created ?? user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_update_user",
            new
            {
                user.Id,
                user.TenantId,
                user.Email,
                NormalizedEmail = user.Email.ToUpperInvariant(),
                user.FirstName,
                user.LastName,
                user.DisplayName,
                user.IsActive,
                user.UpdatedAt
            },
            cancellationToken: cancellationToken);
    }

    public async Task AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_assign_platform_role",
            new { UserId = userId, RoleName = roleName },
            cancellationToken: cancellationToken);
    }

    public async Task AssignOrganizationRoleAsync(Guid tenantId, Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_assign_organization_role",
            new { TenantId = tenantId, OrganizationId = organizationId, UserId = userId, RoleName = roleName },
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_delete_user",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    private sealed class UserHierarchyRow
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? OrganizationName { get; set; }
        public string? ClassroomName { get; set; }
        public int AssociatedChildrenCount { get; set; }
        public string[]? AssignedRoles { get; set; }
    }

    public async Task<IEnumerable<UserHierarchyRecord>> GetUsersHierarchyAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryFunctionAsync<UserHierarchyRow>(
            "fn_get_users_hierarchy",
            new { TenantId = tenantId },
            cancellationToken: cancellationToken);

        var results = new List<UserHierarchyRecord>();
        foreach (var r in rows)
        {
            var rec = new UserHierarchyRecord
            {
                UserId = r.UserId,
                Email = r.Email,
                DisplayName = r.DisplayName,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                OrganizationName = r.OrganizationName,
                ClassroomName = r.ClassroomName,
                AssociatedChildrenCount = r.AssociatedChildrenCount,
                AssignedRoles = r.AssignedRoles?.ToList() ?? new List<string>()
            };

            if (!string.IsNullOrEmpty(r.ClassroomName) && !rec.AssignedRoles.Contains("Teacher", StringComparer.OrdinalIgnoreCase))
            {
                rec.AssignedRoles.Add("Teacher");
            }
            if (r.AssociatedChildrenCount > 0 && !rec.AssignedRoles.Contains("Parent", StringComparer.OrdinalIgnoreCase))
            {
                rec.AssignedRoles.Add("Parent");
            }

            foreach (var role in rec.AssignedRoles)
            {
                rec.CanonicalRoles.Add(EnjoyEveryday.Shared.Authorization.RoleTemplates.Resolve(role));
            }

            results.Add(rec);
        }

        return results;
    }

    private sealed class UserProfileSecurityDetailsRow
    {
        public Guid UserId { get; set; }
        public string? DisplayName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string[]? AssignedRoleNames { get; set; }
        public bool IsClassroomTeacher { get; set; }
        public bool IsParent { get; set; }
    }

    public async Task<UserProfileSecurityDetails?> GetUserProfileSecurityDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleFunctionAsync<UserProfileSecurityDetailsRow>(
            "fn_get_user_profile_security_details",
            new { UserId = userId },
            cancellationToken: cancellationToken);

        if (row == null) return null;

        return new UserProfileSecurityDetails
        {
            UserId = row.UserId,
            DisplayName = row.DisplayName,
            FirstName = row.FirstName,
            LastName = row.LastName,
            AssignedRoleNames = row.AssignedRoleNames?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>(),
            IsClassroomTeacher = row.IsClassroomTeacher,
            IsParent = row.IsParent
        };
    }
}
