using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class RoleAndPermissionMatrixSecurityTests
{
    private Guid _tenantId;
    private Guid _orgAId;
    private Guid _orgBId;
    private Guid _branch1Id;
    private Guid _branch2Id;
    private Guid _class1Id;
    private Guid _class2Id;
    private Guid _child1Id;
    private Guid _child2Id;

    private FakeAccessScopeRepository _scopeRepo = null!;
    private FakeTenantContext _tenantContext = null!;
    private FakeUserContext _userContext = null!;
    private FakeAuditService _auditService = null!;
    private ResourceAuthorizer _authorizer = null!;

    [SetUp]
    public void SetUp()
    {
        _tenantId = Guid.NewGuid();
        _orgAId = Guid.NewGuid();
        _orgBId = Guid.NewGuid();
        _branch1Id = Guid.NewGuid();
        _branch2Id = Guid.NewGuid();
        _class1Id = Guid.NewGuid();
        _class2Id = Guid.NewGuid();
        _child1Id = Guid.NewGuid();
        _child2Id = Guid.NewGuid();

        _scopeRepo = new FakeAccessScopeRepository();
        _tenantContext = new FakeTenantContext { TenantId = _tenantId, IsResolved = true };
        _userContext = new FakeUserContext { IsAuthenticated = true };
        _auditService = new FakeAuditService();

        _authorizer = new ResourceAuthorizer(_scopeRepo, _tenantContext, _userContext, _auditService);

        // Topology setup:
        // Branch 1 is in Org A; Classroom 1 is in Branch 1; Child 1 is in Classroom 1
        _scopeRepo.Branches[_branch1Id] = new ResourceScope(_tenantId, _orgAId, _branch1Id, null);
        _scopeRepo.Classrooms[_class1Id] = new ResourceScope(_tenantId, _orgAId, _branch1Id, _class1Id);
        _scopeRepo.Children[_child1Id] = new ResourceScope(_tenantId, _orgAId, _branch1Id, _class1Id);

        // Branch 2 is in Org B; Classroom 2 is in Branch 2; Child 2 is in Classroom 2
        _scopeRepo.Branches[_branch2Id] = new ResourceScope(_tenantId, _orgBId, _branch2Id, null);
        _scopeRepo.Classrooms[_class2Id] = new ResourceScope(_tenantId, _orgBId, _branch2Id, _class2Id);
        _scopeRepo.Children[_child2Id] = new ResourceScope(_tenantId, _orgBId, _branch2Id, _class2Id);
    }

    // =========================================================================
    // 1. Platform Admin
    // =========================================================================

    [Test]
    public void PlatformAdmin_CannotAccessTenantChildData_WithoutActiveSupportGrant()
    {
        var platformAdminId = Guid.NewGuid();
        _userContext.UserId = platformAdminId;
        _userContext.OrganizationId = null; // Platform admin has no tenant org membership
        _userContext.Roles = new[] { RoleTemplates.PlatformAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.PlatformAdmin).ToList();

        // Platform Admin trying to read Child 1 without a support grant
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireChildAsync(_child1Id, Permissions.ChildView);
        });
    }

    [Test]
    public async Task PlatformAdmin_CanAccessTenantChildData_WithActiveSupportGrant_AndLogsAudit()
    {
        var platformAdminId = Guid.NewGuid();
        _userContext.UserId = platformAdminId;
        _userContext.OrganizationId = null;
        _userContext.Roles = new[] { RoleTemplates.PlatformAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.PlatformAdmin).ToList();

        // Issue active support grant
        var grant = new SupportGrant(
            Guid.NewGuid(), _tenantId, platformAdminId, "Investigating production incident #1042",
            DateTimeOffset.UtcNow.AddHours(2), true);
        _scopeRepo.ActiveSupportGrants.Add(grant);

        // Allowed with audited grant
        Assert.DoesNotThrowAsync(async () =>
        {
            await _authorizer.RequireChildAsync(_child1Id, Permissions.ChildView);
        });

        Assert.That(_auditService.LoggedEntries.Any(e => e.Action == "Support.TenantDataAccess"), Is.True);
    }

    [Test]
    public void PlatformAdmin_CannotAccessTenantChildData_WithExpiredSupportGrant()
    {
        var platformAdminId = Guid.NewGuid();
        _userContext.UserId = platformAdminId;
        _userContext.OrganizationId = null;
        _userContext.Roles = new[] { RoleTemplates.PlatformAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.PlatformAdmin).ToList();

        // Expired grant
        var expiredGrant = new SupportGrant(
            Guid.NewGuid(), _tenantId, platformAdminId, "Expired support ticket",
            DateTimeOffset.UtcNow.AddHours(-1), false);
        _scopeRepo.ActiveSupportGrants.Add(expiredGrant);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireChildAsync(_child1Id, Permissions.ChildView);
        });
    }

    [Test]
    public void PlatformAdmin_CannotAssignRoleToSelf()
    {
        var adminId = Guid.NewGuid();
        _userContext.UserId = adminId;
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.PlatformAdmin).ToList();

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireCanGrantRoleAsync(RoleTemplates.PlatformAdmin, adminId);
        });
    }

    // =========================================================================
    // 2. Organization Admin
    // =========================================================================

    [Test]
    public async Task OrganizationAdmin_HasFullAccessWithinActiveOrganization()
    {
        var orgAdminId = Guid.NewGuid();
        _userContext.UserId = orgAdminId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.OrganizationAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.OrganizationAdmin).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, orgAdminId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, orgAdminId)] = new List<string> { RoleTemplates.OrganizationAdmin };

        // Can access branch, classroom, and child in Org A
        Assert.DoesNotThrowAsync(async () =>
        {
            await _authorizer.RequireBranchAsync(_branch1Id, Permissions.SafetyMapManage);
            await _authorizer.RequireClassroomAsync(_class1Id, Permissions.ClassroomManage);
            await _authorizer.RequireChildAsync(_child1Id, Permissions.ChildManage);
        });
    }

    [Test]
    public void OrganizationAdmin_CannotAccessResourcesInDifferentOrganization()
    {
        var orgAdminId = Guid.NewGuid();
        _userContext.UserId = orgAdminId;
        _userContext.OrganizationId = _orgAId; // Context is Org A
        _userContext.Roles = new[] { RoleTemplates.OrganizationAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.OrganizationAdmin).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, orgAdminId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, orgAdminId)] = new List<string> { RoleTemplates.OrganizationAdmin };

        // Attempting to access Branch 2, which belongs to Org B
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireBranchAsync(_branch2Id, Permissions.SafetyMapManage);
        });

        // Attempting to access Child 2, which belongs to Org B
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireChildAsync(_child2Id, Permissions.ChildView);
        });
    }

    [Test]
    public void OrganizationAdmin_CannotGrantPlatformAdminRole()
    {
        var orgAdminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _userContext.UserId = orgAdminId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.OrganizationAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.OrganizationAdmin).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, orgAdminId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, orgAdminId)] = new List<string> { RoleTemplates.OrganizationAdmin };

        // Cannot escalate to platform admin through org role management
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireCanGrantRoleAsync(RoleTemplates.PlatformAdmin, targetUserId);
        });
    }

    // =========================================================================
    // 3. Branch Admin
    // =========================================================================

    [Test]
    public async Task BranchAdmin_AllowedInAssignedBranch_DeniedInUnassignedBranch()
    {
        var branchAdminId = Guid.NewGuid();
        _userContext.UserId = branchAdminId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.BranchAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.BranchAdmin).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, branchAdminId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, branchAdminId)] = new List<string> { RoleTemplates.BranchAdmin };
        _scopeRepo.AssignedBranches[(_tenantId, _orgAId, branchAdminId)] = new HashSet<Guid> { _branch1Id };

        // Access assigned Branch 1 succeeds
        Assert.DoesNotThrowAsync(async () =>
        {
            await _authorizer.RequireBranchAsync(_branch1Id, Permissions.SafetyMapPublish);
        });

        // Access unassigned Branch 2 is denied
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireBranchAsync(_branch2Id, Permissions.SafetyMapPublish);
        });
    }

    [Test]
    public void BranchAdmin_CannotGrantOrganizationAdminRole_PrivilegeEscalationBlocked()
    {
        var branchAdminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _userContext.UserId = branchAdminId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.BranchAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.BranchAdmin).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, branchAdminId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, branchAdminId)] = new List<string> { RoleTemplates.BranchAdmin };
        _scopeRepo.AssignedBranches[(_tenantId, _orgAId, branchAdminId)] = new HashSet<Guid> { _branch1Id };

        // Branch Admin does not hold org-wide role and lacks OrgAdmin permissions (e.g., org.manage)
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireCanGrantRoleAsync(RoleTemplates.OrganizationAdmin, targetUserId);
        });
    }

    // =========================================================================
    // 4. Daycare Teacher
    // =========================================================================

    [Test]
    public async Task DaycareTeacher_CanAccessAssignedBranch_CannotPerformAdministrativeApprovals()
    {
        var teacherId = Guid.NewGuid();
        _userContext.UserId = teacherId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.DaycareTeacher };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.DaycareTeacher).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, teacherId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, teacherId)] = new List<string> { RoleTemplates.DaycareTeacher };
        _scopeRepo.AssignedClassrooms[(_tenantId, teacherId)] = new HashSet<Guid> { _class1Id };
        _scopeRepo.BranchIdsOfAssignedClassrooms[(_tenantId, teacherId)] = new HashSet<Guid> { _branch1Id };

        // Can view child and record care in assigned classroom/branch
        Assert.DoesNotThrowAsync(async () =>
        {
            await _authorizer.RequireChildAsync(_child1Id, Permissions.CareRecord);
            await _authorizer.RequireBranchAsync(_branch1Id, Permissions.SafetyMapView);
        });

        // Daycare teacher does NOT have consent.review or safety.map.publish permissions in matrix
        Assert.That(_userContext.HasPermission(Permissions.ConsentReview), Is.False);
        Assert.That(_userContext.HasPermission(Permissions.SafetyMapPublish), Is.False);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireBranchAsync(_branch1Id, Permissions.SafetyMapPublish);
        });
    }

    // =========================================================================
    // 5. Classroom Teacher
    // =========================================================================

    [Test]
    public async Task ClassroomTeacher_AllowedInAssignedClassroom_DeniedInOtherClassrooms()
    {
        var teacherId = Guid.NewGuid();
        _userContext.UserId = teacherId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.ClassroomTeacher };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.ClassroomTeacher).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, teacherId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, teacherId)] = new List<string> { RoleTemplates.ClassroomTeacher };
        _scopeRepo.AssignedClassrooms[(_tenantId, teacherId)] = new HashSet<Guid> { _class1Id };

        // Allowed for assigned classroom 1
        Assert.DoesNotThrowAsync(async () =>
        {
            await _authorizer.RequireClassroomAsync(_class1Id, Permissions.ExperienceExecute);
            await _authorizer.RequireChildAsync(_child1Id, Permissions.ChildView);
        });

        // Denied for classroom 2 or child 2
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireClassroomAsync(_class2Id, Permissions.ExperienceExecute);
        });

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireChildAsync(_child2Id, Permissions.ChildView);
        });
    }

    // =========================================================================
    // 6. Parent / Guardian (Relationship-based access)
    // =========================================================================

    [Test]
    public async Task Parent_AllowedAccess_OnlyForVerifiedActiveGuardianRelationship()
    {
        var parentId = Guid.NewGuid();
        _userContext.UserId = parentId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.Parent };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.Parent).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, parentId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, parentId)] = new List<string> { RoleTemplates.Parent };

        // Link verified relationship for Child 1 only
        _scopeRepo.VerifiedGuardianChildren[(_tenantId, parentId)] = new HashSet<Guid> { _child1Id };

        // Allowed for Child 1
        Assert.DoesNotThrowAsync(async () =>
        {
            await _authorizer.RequireGuardianAsync(_child1Id);
            await _authorizer.RequireChildAsync(_child1Id, Permissions.ChildView);
        });

        // Denied for Child 2 (no relationship)
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireGuardianAsync(_child2Id);
        });

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireChildAsync(_child2Id, Permissions.ChildView);
        });
    }

    [Test]
    public void Parent_RevokedOrUnverifiedRelationship_ImmediatelyDenied()
    {
        var parentId = Guid.NewGuid();
        _userContext.UserId = parentId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.Parent };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.Parent).ToList();

        _scopeRepo.ActiveMemberships.Add((_tenantId, _orgAId, parentId));
        _scopeRepo.UserRoles[(_tenantId, _orgAId, parentId)] = new List<string> { RoleTemplates.Parent };

        // Empty verified list simulates unverified or revoked relationship in database
        _scopeRepo.VerifiedGuardianChildren[(_tenantId, parentId)] = new HashSet<Guid>();

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireGuardianAsync(_child1Id);
        });
    }

    [Test]
    public void GlobalParentRole_WithoutVerifiedRelationship_DoesNotGrantAccess()
    {
        var intruderId = Guid.NewGuid();
        _userContext.UserId = intruderId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { "Parent" };
        _userContext.Permissions = new[] { "child.view", "parent.portal.access" };

        // Intruder attempts to view Child 1
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireGuardianAsync(_child1Id);
        });

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireChildAsync(_child1Id, Permissions.ChildView);
        });
    }

    // =========================================================================
    // 7. Revocation & Multi-Tenant Isolation
    // =========================================================================

    [Test]
    public void MembershipRevocation_TakesImmediateEffectOnServer()
    {
        var staffId = Guid.NewGuid();
        _userContext.UserId = staffId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Roles = new[] { RoleTemplates.OrganizationAdmin };
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.OrganizationAdmin).ToList();

        // Membership was revoked in database (no entry in ActiveMemberships)
        _scopeRepo.UserRoles[(_tenantId, _orgAId, staffId)] = new List<string> { RoleTemplates.OrganizationAdmin };

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireBranchAsync(_branch1Id, Permissions.SafetyMapManage);
        });
    }

    [Test]
    public void CrossTenantAccess_IsStrictlyBlocked()
    {
        var foreignTenantId = Guid.NewGuid();
        _tenantContext.TenantId = foreignTenantId; // Caller is authenticated in Foreign Tenant

        var adminId = Guid.NewGuid();
        _userContext.UserId = adminId;
        _userContext.OrganizationId = _orgAId;
        _userContext.Permissions = RoleTemplates.PermissionsOf(RoleTemplates.OrganizationAdmin).ToList();

        // Scope belongs to _tenantId, not foreignTenantId
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _authorizer.RequireBranchAsync(_branch1Id, Permissions.SafetyMapManage);
        });
    }

    // =========================================================================
    // Test Doubles
    // =========================================================================

    private class FakeTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = "Test Tenant";
        public bool IsResolved { get; set; }
    }

    private class FakeUserContext : IUserContext
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = "test@enjoyeveryday.com";
        public bool IsAuthenticated { get; set; }
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
        public Guid? OrganizationId { get; set; }

        public bool HasPermission(string permission) =>
            Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    private class FakeAuditService : IAuditService
    {
        public List<AuditEntry> LoggedEntries { get; } = new();

        public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            LoggedEntries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private class FakeAccessScopeRepository : IAccessScopeRepository
    {
        public Dictionary<Guid, ResourceScope> Branches { get; } = new();
        public Dictionary<Guid, ResourceScope> Classrooms { get; } = new();
        public Dictionary<Guid, ResourceScope> Children { get; } = new();

        public HashSet<(Guid TenantId, Guid OrgId, Guid UserId)> ActiveMemberships { get; } = new();
        public Dictionary<(Guid TenantId, Guid OrgId, Guid UserId), List<string>> UserRoles { get; } = new();
        public Dictionary<(Guid TenantId, Guid OrgId, Guid UserId), HashSet<Guid>> AssignedBranches { get; } = new();
        public Dictionary<(Guid TenantId, Guid UserId), HashSet<Guid>> AssignedClassrooms { get; } = new();
        public Dictionary<(Guid TenantId, Guid UserId), HashSet<Guid>> BranchIdsOfAssignedClassrooms { get; } = new();
        public Dictionary<(Guid TenantId, Guid UserId), HashSet<Guid>> VerifiedGuardianChildren { get; } = new();
        public List<SupportGrant> ActiveSupportGrants { get; } = new();

        public Task<ResourceScope?> GetBranchScopeAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
        {
            Branches.TryGetValue(branchId, out var scope);
            return Task.FromResult(scope);
        }

        public Task<ResourceScope?> GetClassroomScopeAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default)
        {
            Classrooms.TryGetValue(classroomId, out var scope);
            return Task.FromResult(scope);
        }

        public Task<ResourceScope?> GetChildScopeAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
        {
            Children.TryGetValue(childId, out var scope);
            return Task.FromResult(scope);
        }

        public Task<bool> HasActiveMembershipAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(ActiveMemberships.Contains((tenantId, organizationId, userId)));
        }

        public Task<IReadOnlyList<string>> GetOrganizationRoleNamesAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
        {
            if (UserRoles.TryGetValue((tenantId, organizationId, userId), out var roles))
            {
                return Task.FromResult<IReadOnlyList<string>>(roles);
            }
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        public Task<IReadOnlySet<Guid>> GetAssignedBranchIdsAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default)
        {
            if (AssignedBranches.TryGetValue((tenantId, organizationId, userId), out var branches))
            {
                return Task.FromResult<IReadOnlySet<Guid>>(branches);
            }
            return Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        }

        public Task<IReadOnlySet<Guid>> GetAssignedClassroomIdsAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
        {
            if (AssignedClassrooms.TryGetValue((tenantId, userId), out var classrooms))
            {
                return Task.FromResult<IReadOnlySet<Guid>>(classrooms);
            }
            return Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        }

        public Task<IReadOnlySet<Guid>> GetBranchIdsOfAssignedClassroomsAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
        {
            if (BranchIdsOfAssignedClassrooms.TryGetValue((tenantId, userId), out var branches))
            {
                return Task.FromResult<IReadOnlySet<Guid>>(branches);
            }
            return Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        }

        public Task<bool> HasVerifiedGuardianRelationshipAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken ct = default)
        {
            if (VerifiedGuardianChildren.TryGetValue((tenantId, userId), out var children))
            {
                return Task.FromResult(children.Contains(childId));
            }
            return Task.FromResult(false);
        }

        public Task<SupportGrant?> GetActiveSupportGrantAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var grant = ActiveSupportGrants.FirstOrDefault(g =>
                g.TenantId == tenantId &&
                g.GrantedToUserId == userId &&
                g.ExpiresAt >= now);
            return Task.FromResult<SupportGrant?>(grant);
        }
    }
}
