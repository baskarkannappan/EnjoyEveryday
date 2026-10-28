using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.Daycare.Web.Services;

public sealed class UserRoleProfile
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public bool IsOrgAdmin { get; set; }
    public bool IsBranchAdmin { get; set; }
    public bool IsLeadTeacher { get; set; }
    public bool IsTeacher { get; set; }
    public string PrimaryRoleName { get; set; } = "Teacher";
    public List<string> AvailableViews { get; set; } = new();
    public HashSet<Guid> AssignedBranchIds { get; set; } = new();
    public HashSet<Guid> AssignedClassroomIds { get; set; } = new();
}

public interface IPageAuthorizationService
{
    Task<UserRoleProfile> GetUserProfileAsync(CancellationToken ct = default);
    Task<bool> CanAccessRouteAsync(string relativePath, CancellationToken ct = default);
    Task<bool> CanAccessBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<bool> CanAccessClassroomAsync(Guid classroomId, CancellationToken ct = default);
    Task<bool> EnsureAuthorizedAsync(string relativePath, Guid? resourceId = null, string resourceType = "Organization", CancellationToken ct = default);
}

public sealed class PageAuthorizationService : IPageAuthorizationService
{
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IAccessScopeRepository _scopeRepo;
    private readonly IResourceAuthorizer _resourceAuthorizer;
    private readonly NavigationManager _navigation;
    private readonly IOrganizationPermissionService _orgPerms;

    // Cache profile per scope
    private UserRoleProfile? _cachedProfile;

    public PageAuthorizationService(
        IUserContext userContext,
        ITenantContext tenantContext,
        IAccessScopeRepository scopeRepo,
        IResourceAuthorizer resourceAuthorizer,
        NavigationManager navigation,
        IOrganizationPermissionService orgPerms)
    {
        _userContext = userContext;
        _tenantContext = tenantContext;
        _scopeRepo = scopeRepo;
        _resourceAuthorizer = resourceAuthorizer;
        _navigation = navigation;
        _orgPerms = orgPerms;
    }

    public async Task<UserRoleProfile> GetUserProfileAsync(CancellationToken ct = default)
    {
        if (_cachedProfile != null) return _cachedProfile;

        var profile = new UserRoleProfile
        {
            UserId = _userContext.UserId,
            Email = _userContext.Email ?? string.Empty,
            IsAuthenticated = _userContext.IsAuthenticated
        };

        if (!profile.IsAuthenticated || _tenantContext.TenantId == Guid.Empty)
        {
            _cachedProfile = profile;
            return profile;
        }

        var tenantId = _tenantContext.TenantId;
        var userId = profile.UserId;
        var orgId = _userContext.OrganizationId ?? Guid.Empty;
        profile.OrganizationId = orgId;

        // Fetch live scopes from database
        var classroomIds = await _scopeRepo.GetAssignedClassroomIdsAsync(tenantId, userId, ct);
        profile.AssignedClassroomIds = new HashSet<Guid>(classroomIds);

        if (orgId != Guid.Empty)
        {
            var branchIds = await _scopeRepo.GetAssignedBranchIdsAsync(tenantId, orgId, userId, ct);
            profile.AssignedBranchIds = new HashSet<Guid>(branchIds);
        }

        var roles = (_userContext.Roles ?? Enumerable.Empty<string>()).ToList();

        // 0. Platform Admin check
        profile.IsPlatformAdmin = roles.Any(r =>
            r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase));

        // 1. Organization Admin check
        profile.IsOrgAdmin = profile.IsPlatformAdmin || roles.Any(r =>
            r.Equals(RoleTemplates.OrganizationOwner, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationAdmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Daycare Admin", StringComparison.OrdinalIgnoreCase))
            || _userContext.HasPermission(Permissions.OrganizationManage);

        // 2. Branch Admin check
        profile.IsBranchAdmin = profile.IsOrgAdmin || roles.Any(r =>
            r.Equals(RoleTemplates.BranchAdmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Branch Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Branch Director", StringComparison.OrdinalIgnoreCase))
            || _userContext.HasPermission(Permissions.BranchManage);

        // 3. Lead Teacher check
        profile.IsLeadTeacher = roles.Any(r =>
            r.Equals(RoleTemplates.LeadTeacher, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Lead Teacher", StringComparison.OrdinalIgnoreCase))
            || _userContext.HasPermission(Permissions.ObservationApprove)
            || _userContext.HasPermission(Permissions.CareHandover);

        // 4. Teacher check
        profile.IsTeacher = profile.IsOrgAdmin || profile.IsBranchAdmin || profile.IsLeadTeacher || roles.Any(r =>
            r.Equals(RoleTemplates.DaycareTeacher, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.ClassroomTeacher, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Teacher", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("AssistantTeacher", StringComparison.OrdinalIgnoreCase))
            || profile.AssignedClassroomIds.Count > 0
            || _userContext.HasPermission(Permissions.AttendanceRecord);

        // Available dashboard views:
        profile.AvailableViews.Clear();
        if (profile.IsTeacher) profile.AvailableViews.Add("Teacher");
        if (profile.IsLeadTeacher) profile.AvailableViews.Add("LeadTeacher");
        if (profile.IsBranchAdmin) profile.AvailableViews.Add("BranchAdmin");
        if (profile.IsOrgAdmin) profile.AvailableViews.Add("OrgAdmin");

        if (profile.AvailableViews.Count == 0)
        {
            profile.AvailableViews.Add("Teacher");
        }

        // Primary role name
        if (profile.IsOrgAdmin) profile.PrimaryRoleName = "Organization Admin";
        else if (profile.IsBranchAdmin) profile.PrimaryRoleName = "Branch Admin";
        else if (profile.IsLeadTeacher) profile.PrimaryRoleName = "Lead Teacher";
        else profile.PrimaryRoleName = "Teacher";

        _cachedProfile = profile;
        return profile;
    }

    public async Task<bool> CanAccessRouteAsync(string relativePath, CancellationToken ct = default)
    {
        var profile = await GetUserProfileAsync(ct);
        if (!profile.IsAuthenticated) return false;

        // Platform Admin can access everything
        if (profile.IsPlatformAdmin) return true;

        var cleanPath = NormalizePath(relativePath);

        // Matrix page itself is strictly OrgAdmin exclusive:
        if (cleanPath == "/management/roles" || cleanPath == "/roles")
        {
            return profile.IsOrgAdmin;
        }

        // Check organization-specific permission matrix if scoped to an organization
        if (profile.OrganizationId != Guid.Empty && _tenantContext.TenantId != Guid.Empty)
        {
            string roleKey = profile.IsOrgAdmin ? "OrganizationAdmin"
                           : profile.IsBranchAdmin ? "BranchAdmin"
                           : profile.IsLeadTeacher ? "LeadTeacher"
                           : "Teacher";

            var customAllowed = await _orgPerms.CheckRouteCustomAllowedAsync(
                _tenantContext.TenantId,
                profile.OrganizationId,
                roleKey,
                cleanPath,
                ct);

            if (customAllowed.HasValue)
            {
                // Org admins retain access to core settings & dashboards
                if (profile.IsOrgAdmin && !customAllowed.Value && (cleanPath == "/management/dashboard" || cleanPath == "/settings"))
                {
                    return true;
                }

                return customAllowed.Value;
            }
        }

        // OrgAdmin exclusive routes:
        if (IsOrgAdminRoute(cleanPath))
        {
            return profile.IsOrgAdmin;
        }

        // BranchAdmin & OrgAdmin routes:
        if (IsBranchAdminRoute(cleanPath))
        {
            return profile.IsBranchAdmin || profile.IsOrgAdmin;
        }

        // All active daycare staff permitted (scoped internally):
        return true;
    }

    public async Task<bool> CanAccessBranchAsync(Guid branchId, CancellationToken ct = default)
    {
        var profile = await GetUserProfileAsync(ct);
        if (!profile.IsAuthenticated) return false;

        // Org Admin can access any branch in their active organization
        if (profile.IsOrgAdmin) return true;

        // Branch Admin must have explicit assignment to this branch
        if (profile.AssignedBranchIds.Contains(branchId)) return true;

        // Teacher / Lead Teacher can access if their assigned classrooms belong to this branch
        var tenantId = _tenantContext.TenantId;
        var ownBranches = await _scopeRepo.GetBranchIdsOfAssignedClassroomsAsync(tenantId, profile.UserId, ct);
        return ownBranches.Contains(branchId);
    }

    public async Task<bool> CanAccessClassroomAsync(Guid classroomId, CancellationToken ct = default)
    {
        var profile = await GetUserProfileAsync(ct);
        if (!profile.IsAuthenticated) return false;

        if (profile.IsOrgAdmin) return true;

        // Teacher or Lead Teacher must be assigned
        if (profile.AssignedClassroomIds.Contains(classroomId)) return true;

        // Branch Admin can access if room is in an assigned branch
        if (profile.IsBranchAdmin)
        {
            var tenantId = _tenantContext.TenantId;
            var scope = await _scopeRepo.GetClassroomScopeAsync(tenantId, classroomId, ct);
            if (scope?.BranchId.HasValue == true && profile.AssignedBranchIds.Contains(scope.BranchId.Value))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> EnsureAuthorizedAsync(
        string relativePath,
        Guid? resourceId = null,
        string resourceType = "Organization",
        CancellationToken ct = default)
    {
        var profile = await GetUserProfileAsync(ct);
        if (!profile.IsAuthenticated)
        {
            _navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(_navigation.Uri)}", forceLoad: false);
            return false;
        }

        bool routeAllowed = await CanAccessRouteAsync(relativePath, ct);
        if (!routeAllowed)
        {
            _navigation.NavigateTo("/access-denied?reason=unauthorized_route", forceLoad: false);
            return false;
        }

        if (resourceId.HasValue && resourceId.Value != Guid.Empty)
        {
            if (resourceType.Equals("Branch", StringComparison.OrdinalIgnoreCase))
            {
                bool branchAllowed = await CanAccessBranchAsync(resourceId.Value, ct);
                if (!branchAllowed)
                {
                    _navigation.NavigateTo("/access-denied?reason=unauthorized_branch", forceLoad: false);
                    return false;
                }
            }
            else if (resourceType.Equals("Classroom", StringComparison.OrdinalIgnoreCase))
            {
                bool roomAllowed = await CanAccessClassroomAsync(resourceId.Value, ct);
                if (!roomAllowed)
                {
                    _navigation.NavigateTo("/access-denied?reason=unauthorized_classroom", forceLoad: false);
                    return false;
                }
            }
        }

        return true;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var clean = path.Split('?')[0].Trim().ToLowerInvariant();
        if (!clean.StartsWith('/')) clean = "/" + clean;
        return clean;
    }

    private static bool IsOrgAdminRoute(string path)
    {
        return path == "/management/dashboard"
            || path == "/org-dashboard"
            || path == "/management/billing"
            || path == "/billing"
            || path == "/billing/invoices"
            || path == "/billing/reports"
            || path == "/management/branches/new"
            || path == "/settings"
            || path == "/organization/settings"
            || path == "/organization/profile"
            || path == "/organization"
            || path == "/organization/branches"
            || path == "/management/roles"
            || path == "/roles"
            || path == "/rhythm-history"
            || path == "/audit";
    }

    private static bool IsBranchAdminRoute(string path)
    {
        return path == "/management/branch-dashboard"
            || path == "/branch-dashboard"
            || path == "/management/roster"
            || path == "/management/enrollment"
            || path == "/enrollment"
            || path == "/classrooms/new"
            || path.StartsWith("/classrooms/edit")
            || path == "/teachers/new"
            || path.StartsWith("/teachers/edit")
            || path.StartsWith("/management/branches")
            || path == "/users-by-role"
            || path == "/management/users-by-role"
            || path == "/materials-orders";
    }
}
