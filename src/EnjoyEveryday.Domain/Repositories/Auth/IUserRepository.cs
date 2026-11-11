using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult<User?>(null);
    Task<IEnumerable<User>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByRoleAsync(Guid tenantId, string roleName, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    /// <summary>Global PLATFORM roles only (user_roles). Organization roles must use <see cref="AssignOrganizationRoleAsync"/>.</summary>
    Task AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    /// <summary>Assigns a role inside ONE organization (organization_memberships + user_organization_roles).</summary>
    Task AssignOrganizationRoleAsync(Guid tenantId, Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Assigns a user to a specific physical branch location (user_branch_assignments).</summary>
    Task AssignBranchToUserAsync(Guid tenantId, Guid organizationId, Guid branchId, Guid userId, Guid? assignedBy = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Revokes a user's assignment to a physical branch location.</summary>
    Task RevokeBranchAssignmentAsync(Guid tenantId, Guid organizationId, Guid branchId, Guid userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Removes a teacher assignment from a specific classroom.</summary>
    Task RemoveTeacherFromClassroomAsync(Guid tenantId, Guid classroomId, Guid userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Promotes a teacher to Lead Teacher in an organization/branch context with classroom assignments.</summary>
    Task PromoteTeacherToLeadAsync(Guid tenantId, Guid organizationId, Guid branchId, Guid userId, Guid promotedBy, IEnumerable<Guid> classroomIds, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Updates classroom assignments for a teacher/lead teacher within a specific branch.</summary>
    Task UpdateTeacherClassroomsAsync(Guid tenantId, Guid organizationId, Guid branchId, Guid userId, IEnumerable<Guid> classroomIds, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Deactivates a user's membership and branch/classroom assignments in ONE organization only.</summary>
    Task RemoveUserFromOrganizationAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Gets all users in a tenant with their resolved role hierarchy, organization assignments, and associations.</summary>
    Task<IEnumerable<UserHierarchyRecord>> GetUsersHierarchyAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<UserHierarchyRecord>());

    /// <summary>Gets user profile security details, assigned roles, and authority flags.</summary>
    Task<UserProfileSecurityDetails?> GetUserProfileSecurityDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<UserProfileSecurityDetails?>(null);
}

