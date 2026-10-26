using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByRoleAsync(Guid tenantId, string roleName, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    /// <summary>Global PLATFORM roles only (user_roles). Organization roles must use <see cref="AssignOrganizationRoleAsync"/>.</summary>
    Task AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    /// <summary>Assigns a role inside ONE organization (organization_memberships + user_organization_roles).</summary>
    Task AssignOrganizationRoleAsync(Guid tenantId, Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Gets all users in a tenant with their resolved role hierarchy, organization assignments, and associations.</summary>
    Task<IEnumerable<UserHierarchyRecord>> GetUsersHierarchyAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<UserHierarchyRecord>());

    /// <summary>Gets user profile security details, assigned roles, and authority flags.</summary>
    Task<UserProfileSecurityDetails?> GetUserProfileSecurityDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<UserProfileSecurityDetails?>(null);
}

