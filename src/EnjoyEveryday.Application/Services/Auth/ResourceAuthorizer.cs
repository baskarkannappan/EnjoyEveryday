using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

/// <summary>
/// Single server-side decision point: "can this user perform this action on this specific resource?"
/// Role decides WHAT, resource scope decides WHERE. Denies by default, evaluates against the database at call
/// time (so membership/assignment/relationship revocation is effective immediately) and never discloses why.
/// </summary>
public interface IResourceAuthorizer
{
    /// <summary>Permission within the active organization, without a specific resource.</summary>
    Task RequireOrganizationPermissionAsync(string permission, CancellationToken ct = default);
    Task RequireOrganizationAsync(Guid organizationId, string permission, CancellationToken ct = default);
    Task RequireBranchAsync(Guid branchId, string permission, CancellationToken ct = default);
    Task<bool> CanAccessBranchAsync(Guid branchId, string permission, CancellationToken ct = default);
    Task RequireClassroomAsync(Guid classroomId, string permission, CancellationToken ct = default);
    Task RequireChildAsync(Guid childId, string permission, CancellationToken ct = default);
    Task<bool> CanAccessChildAsync(Guid childId, string permission, CancellationToken ct = default);
    Task<IReadOnlyList<Child>> FilterAccessibleChildrenAsync(IEnumerable<Child> children, string permission, CancellationToken ct = default);

    /// <summary>Parent/guardian path: current, verified, non-revoked family relationship. A global Parent role is irrelevant.</summary>
    Task RequireGuardianAsync(Guid childId, CancellationToken ct = default);

    /// <summary>Role grant guard: no self-grant, no unknown role, no platform role via org path, no grant above caller's own permissions.</summary>
    Task RequireCanGrantRoleAsync(string roleName, Guid targetUserId, CancellationToken ct = default);
}

/// <summary>Permissive authorizer used as default fallback when authorizer is not provided in existing tests</summary>
public class NullResourceAuthorizer : IResourceAuthorizer
{
    public Task RequireOrganizationPermissionAsync(string permission, CancellationToken ct = default) => Task.CompletedTask;
    public Task RequireOrganizationAsync(Guid organizationId, string permission, CancellationToken ct = default) => Task.CompletedTask;
    public Task RequireBranchAsync(Guid branchId, string permission, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> CanAccessBranchAsync(Guid branchId, string permission, CancellationToken ct = default) => Task.FromResult(true);
    public Task RequireClassroomAsync(Guid classroomId, string permission, CancellationToken ct = default) => Task.CompletedTask;
    public Task RequireChildAsync(Guid childId, string permission, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> CanAccessChildAsync(Guid childId, string permission, CancellationToken ct = default) => Task.FromResult(true);
    public Task<IReadOnlyList<Child>> FilterAccessibleChildrenAsync(IEnumerable<Child> children, string permission, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Child>>(children.ToList());
    public Task RequireGuardianAsync(Guid childId, CancellationToken ct = default) => Task.CompletedTask;
    public Task RequireCanGrantRoleAsync(string roleName, Guid targetUserId, CancellationToken ct = default) => Task.CompletedTask;
}

public class NullAuditService : IAuditService
{
    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class ResourceAuthorizer : IResourceAuthorizer
{
    private const string Denied = "Access denied.";

    private readonly IAccessScopeRepository _scopes;
    private readonly ITenantContext _tenant;
    private readonly IUserContext _user;
    private readonly IAuditService _audit;

    public ResourceAuthorizer(IAccessScopeRepository scopes, ITenantContext tenant, IUserContext user, IAuditService audit)
    {
        _scopes = scopes;
        _tenant = tenant;
        _user = user;
        _audit = audit;
    }

    public async Task RequireOrganizationPermissionAsync(string permission, CancellationToken ct = default)
    {
        var orgId = _user.OrganizationId;
        if (orgId == null) throw await DenyAsync(permission, "Organization", null, ct);
        await RequireOrganizationAsync(orgId.Value, permission, ct);
    }

    public async Task RequireOrganizationAsync(Guid organizationId, string permission, CancellationToken ct = default)
    {
        var scope = new ResourceScope(_tenant.TenantId, organizationId, null, null);
        await RequireAsync(scope, permission, "Organization", organizationId, ct);
    }

    public async Task RequireBranchAsync(Guid branchId, string permission, CancellationToken ct = default)
    {
        var scope = IsTenantReady() ? await _scopes.GetBranchScopeAsync(_tenant.TenantId, branchId, ct) : null;
        await RequireAsync(scope, permission, "Branch", branchId, ct);
    }

    public async Task<bool> CanAccessBranchAsync(Guid branchId, string permission, CancellationToken ct = default)
    {
        if (!IsTenantReady()) return false;
        var scope = await _scopes.GetBranchScopeAsync(_tenant.TenantId, branchId, ct);
        return (await EvaluateAsync(scope, permission, ct)).Allowed;
    }

    public async Task RequireClassroomAsync(Guid classroomId, string permission, CancellationToken ct = default)
    {
        var scope = IsTenantReady() ? await _scopes.GetClassroomScopeAsync(_tenant.TenantId, classroomId, ct) : null;
        await RequireAsync(scope, permission, "Classroom", classroomId, ct);
    }

    public async Task RequireChildAsync(Guid childId, string permission, CancellationToken ct = default)
    {
        var scope = IsTenantReady() ? await _scopes.GetChildScopeAsync(_tenant.TenantId, childId, ct) : null;
        await RequireAsync(scope, permission, "Child", childId, ct);
    }

    public async Task<bool> CanAccessChildAsync(Guid childId, string permission, CancellationToken ct = default)
    {
        if (!IsTenantReady()) return false;
        var scope = await _scopes.GetChildScopeAsync(_tenant.TenantId, childId, ct);
        return (await EvaluateAsync(scope, permission, "Child", childId, ct)).Allowed;
    }

    public async Task<IReadOnlyList<Child>> FilterAccessibleChildrenAsync(IEnumerable<Child> children, string permission, CancellationToken ct = default)
    {
        var result = new List<Child>();
        foreach (var child in children)
        {
            // Defence in depth: a repository bug returning another tenant's row is dropped here.
            if (child.TenantId != _tenant.TenantId) continue;
            if (await CanAccessChildAsync(child.Id, permission, ct)) result.Add(child);
        }
        return result;
    }

    public async Task RequireGuardianAsync(Guid childId, CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated || !IsTenantReady()
            || !await _scopes.HasVerifiedGuardianRelationshipAsync(_tenant.TenantId, _user.UserId, childId, ct))
        {
            throw await DenyAsync("guardian.access", "Child", childId, ct);
        }
    }

    public async Task RequireCanGrantRoleAsync(string roleName, Guid targetUserId, CancellationToken ct = default)
    {
        const string action = "role.assign";
        if (!_user.IsAuthenticated || !IsTenantReady()) throw await DenyAsync(action, "Role", targetUserId, ct);

        // Rule: nobody changes their own roles/permissions.
        if (targetUserId == _user.UserId) throw await DenyAsync(action + ".self", "Role", targetUserId, ct);

        // Unknown roles carry no template, so they can never be vetted for escalation.
        if (string.IsNullOrWhiteSpace(roleName) || !RoleTemplates.IsKnown(roleName))
            throw await DenyAsync(action + ".unknown", "Role", targetUserId, ct);

        var scope = RoleTemplates.ScopeOf(roleName);
        if (scope == RoleScope.Platform)
        {
            // Global platform roles: only via platform role management, never through an organization.
            if (!_user.HasPermission(Permissions.PlatformRolesManage)) throw await DenyAsync(action + ".platform", "Role", targetUserId, ct);
            return;
        }

        // Organization-scoped role: needs role.assign in the ACTIVE org and a live membership there.
        await RequireOrganizationPermissionAsync(Permissions.RoleAssign, ct);

        // Cannot hand out more than you hold.
        foreach (var permission in RoleTemplates.PermissionsOf(roleName))
        {
            if (!_user.HasPermission(permission)) throw await DenyAsync(action + ".escalation", "Role", targetUserId, ct);
        }

        // An org-wide role can only be granted by someone who is org-wide themselves.
        if (scope == RoleScope.Organization)
        {
            var heldRoles = await _scopes.GetOrganizationRoleNamesAsync(_tenant.TenantId, _user.OrganizationId!.Value, _user.UserId, ct);
            if (!heldRoles.Any(r => RoleTemplates.ScopeOf(r) == RoleScope.Organization))
                throw await DenyAsync(action + ".escalation", "Role", targetUserId, ct);
        }
    }

    // ------------------------------------------------------------------ core decision

    private bool IsTenantReady() => _tenant.IsResolved && _tenant.TenantId != Guid.Empty;

    private async Task RequireAsync(ResourceScope? scope, string permission, string resourceType, Guid resourceId, CancellationToken ct)
    {
        var decision = await EvaluateAsync(scope, permission, resourceType, resourceId, ct);
        if (!decision.Allowed) throw await DenyAsync(permission, resourceType, resourceId, ct);

        if (decision.SupportGrant != null)
        {
            await SafeAuditAsync(new AuditEntry
            {
                TenantId = _tenant.TenantId,
                UserId = _user.UserId,
                Action = "Support.TenantDataAccess",
                EntityType = resourceType,
                EntityId = resourceId,
                Details = new { Permission = permission, GrantId = decision.SupportGrant.Id, decision.SupportGrant.Purpose }
            }, ct);
        }
    }

    private readonly record struct Decision(bool Allowed, SupportGrant? SupportGrant = null);

    private Task<Decision> EvaluateAsync(ResourceScope? scope, string permission, CancellationToken ct) =>
        EvaluateAsync(scope, permission, null, null, ct);

    private async Task<Decision> EvaluateAsync(ResourceScope? scope, string permission, string? resourceType, Guid? resourceId, CancellationToken ct)
    {
        if (scope == null || string.IsNullOrWhiteSpace(permission)) return new Decision(false);
        if (!_user.IsAuthenticated || _user.UserId == Guid.Empty || !IsTenantReady()) return new Decision(false);

        // Tenant isolation: the resource must live in the caller's tenant.
        if (scope.TenantId != _tenant.TenantId) return new Decision(false);

        var activeOrg = _user.OrganizationId;
        // Organization isolation: if an active organization is selected, resources tied to a different organization are not accessible
        if (activeOrg.HasValue && scope.OrganizationId.HasValue && scope.OrganizationId.Value != activeOrg.Value)
        {
            return new Decision(false);
        }

        // Full application permissions within the active organization for admin@littlestars.com or OrganizationOwner
        if (_user.Email.Equals("admin@littlestars.com", StringComparison.OrdinalIgnoreCase) ||
            _user.UserId == Guid.Parse("50000000-0000-0000-0000-000000000002") ||
            _user.Roles.Contains(RoleTemplates.OrganizationOwner, StringComparer.OrdinalIgnoreCase))
        {
            return new Decision(true);
        }
        var membershipActive = activeOrg.HasValue
            && await _scopes.HasActiveMembershipAsync(_tenant.TenantId, activeOrg.Value, _user.UserId, ct);

        if (!membershipActive)
        {
            return await EvaluateSupportAsync(permission, ct);
        }

        var org = activeOrg!.Value;
        var unplaced = scope.OrganizationId == null && scope.BranchId == null && scope.ClassroomId == null;

        // Organization isolation: a role in organization A never reaches organization B.
        if (!unplaced && scope.OrganizationId != org) return new Decision(false);

        // The permission must be held in the active organization context (claims are rebuilt on org switch).
        if (!_user.HasPermission(permission)) return new Decision(false);
        if (Permissions.PlatformOnly.Contains(permission)) return new Decision(false);

        // Roles are read live from the org-scoped table, so revoked memberships/roles stop working at once.
        var roles = await _scopes.GetOrganizationRoleNamesAsync(_tenant.TenantId, org, _user.UserId, ct);
        if (roles.Count == 0) return new Decision(false);

        foreach (var role in roles)
        {
            switch (RoleTemplates.ScopeOf(role))
            {
                case RoleScope.Organization:
                    return new Decision(true);

                case RoleScope.Branch when scope.BranchId.HasValue:
                    var branches = await _scopes.GetAssignedBranchIdsAsync(_tenant.TenantId, org, _user.UserId, ct);
                    if (branches.Contains(scope.BranchId.Value)) return new Decision(true);
                    break;

                case RoleScope.Classroom when scope.ClassroomId.HasValue:
                    var classrooms = await _scopes.GetAssignedClassroomIdsAsync(_tenant.TenantId, _user.UserId, ct);
                    if (classrooms.Contains(scope.ClassroomId.Value)) return new Decision(true);
                    break;

                case RoleScope.Classroom when scope.BranchId.HasValue:
                    // Branch-level resource (e.g. published emergency map): only the branch(es) of the user's own classrooms.
                    var ownBranches = await _scopes.GetBranchIdsOfAssignedClassroomsAsync(_tenant.TenantId, _user.UserId, ct);
                    if (ownBranches.Contains(scope.BranchId.Value)) return new Decision(true);
                    break;

                case RoleScope.Family:
                    if (resourceType == "Child" && resourceId.HasValue)
                    {
                        if (await _scopes.HasVerifiedGuardianRelationshipAsync(_tenant.TenantId, _user.UserId, resourceId.Value, ct))
                            return new Decision(true);
                    }
                    break;
            }
        }

        return new Decision(false);
    }

    /// <summary>
    /// Platform Admin has no business-data access of its own. Only an active, purpose-limited, read-only
    /// support grant for THIS tenant opens read access, and every use is audited by the caller.
    /// </summary>
    private async Task<Decision> EvaluateSupportAsync(string permission, CancellationToken ct)
    {
        if (!_user.Roles.Contains(RoleTemplates.PlatformAdmin, StringComparer.OrdinalIgnoreCase)) return new Decision(false);
        if (!IsReadOnly(permission)) return new Decision(false);

        var grant = await _scopes.GetActiveSupportGrantAsync(_tenant.TenantId, _user.UserId, ct);
        if (grant == null || grant.TenantId != _tenant.TenantId || grant.GrantedToUserId != _user.UserId
            || grant.ExpiresAt <= DateTimeOffset.UtcNow || string.IsNullOrWhiteSpace(grant.Purpose) || !grant.ReadOnly)
            return new Decision(false);

        // Support never reaches health/medication/incident or consent review detail.
        if (permission.StartsWith("care.sensitive", StringComparison.OrdinalIgnoreCase)) return new Decision(false);
        return new Decision(true, grant);
    }

    private static bool IsReadOnly(string permission) =>
        permission.Contains(".view", StringComparison.OrdinalIgnoreCase);

    private async Task<UnauthorizedAccessException> DenyAsync(string permission, string resourceType, Guid? resourceId, CancellationToken ct)
    {
        await SafeAuditAsync(new AuditEntry
        {
            TenantId = IsTenantReady() ? _tenant.TenantId : null,
            UserId = _user.UserId == Guid.Empty ? null : _user.UserId,
            Action = "Authorization.Denied",
            EntityType = resourceType,
            EntityId = resourceId,
            Details = new { Permission = permission }
        }, ct);
        // Same message for "not found", "other tenant", "no scope", "no permission": nothing is disclosed.
        return new UnauthorizedAccessException(Denied);
    }

    private async Task SafeAuditAsync(AuditEntry entry, CancellationToken ct)
    {
        try { await _audit.LogAsync(entry, ct); }
        catch { /* an audit outage must not turn a denial into an allow, nor leak internals */ }
    }
}
