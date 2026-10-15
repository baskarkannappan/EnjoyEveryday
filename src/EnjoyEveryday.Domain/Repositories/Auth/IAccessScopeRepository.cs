namespace EnjoyEveryday.Domain.Repositories;

/// <summary>Where a resource sits in the hierarchy. Null members mean "not found in this tenant".</summary>
public sealed record ResourceScope(Guid TenantId, Guid? OrganizationId, Guid? BranchId, Guid? ClassroomId);

public sealed record SupportGrant(Guid Id, Guid TenantId, Guid GrantedToUserId, string Purpose, DateTimeOffset ExpiresAt, bool ReadOnly);

/// <summary>
/// Read-only lookups that feed authorization decisions. Every method is tenant-filtered and evaluated
/// at call time (no caching) so revocations take effect on the next protected action.
/// Reuses: organization_memberships, user_organization_roles, classroom_teachers, family_relationships,
/// branches/classrooms/children. New tables: user_branch_assignments, support_access_grants.
/// </summary>
public interface IAccessScopeRepository
{
    Task<bool> HasActiveMembershipAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default);

    /// <summary>Role names held by the user in ONE organization (user_organization_roles), never global roles.</summary>
    Task<IReadOnlyList<string>> GetOrganizationRoleNamesAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlySet<Guid>> GetAssignedBranchIdsAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlySet<Guid>> GetAssignedClassroomIdsAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    /// <summary>Branches that contain at least one classroom the user is assigned to.</summary>
    Task<IReadOnlySet<Guid>> GetBranchIdsOfAssignedClassroomsAsync(Guid tenantId, Guid userId, CancellationToken ct = default);

    Task<ResourceScope?> GetBranchScopeAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);
    Task<ResourceScope?> GetClassroomScopeAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default);
    /// <summary>Scope of a child's current classroom; ClassroomId null if the child is not placed.</summary>
    Task<ResourceScope?> GetChildScopeAsync(Guid tenantId, Guid childId, CancellationToken ct = default);

    /// <summary>True only for a Verified, not revoked relationship to an active child.</summary>
    Task<bool> HasVerifiedGuardianRelationshipAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken ct = default);

    Task<SupportGrant?> GetActiveSupportGrantAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
}
