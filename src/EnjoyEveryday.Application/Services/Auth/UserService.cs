using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IAuditService _auditService;
    private readonly IAccessScopeRepository? _scopeRepo;

    public UserService(IUserRepository userRepository, ITenantContext tenantContext, IUserContext userContext)
        : this(userRepository, tenantContext, userContext, new NullResourceAuthorizer(), new NullAuditService(), null)
    {
    }

    public UserService(IUserRepository userRepository, ITenantContext tenantContext, IUserContext userContext,
        IResourceAuthorizer authorizer, IAuditService auditService, IAccessScopeRepository? scopeRepo = null)
    {
        _userRepository = userRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _authorizer = authorizer;
        _auditService = auditService;
        _scopeRepo = scopeRepo;
    }

    /// <summary>
    /// Assigns a role to an existing user in the ACTIVE organization. Blocks self-grants, unknown roles,
    /// platform roles via the org path and any role that exceeds the caller's own permissions. Audited.
    /// </summary>
    public async Task AssignOrganizationRoleAsync(Guid targetUserId, string roleName, CancellationToken cancellationToken = default)
    {
        await _authorizer.RequireCanGrantRoleAsync(roleName, targetUserId, cancellationToken);

        var target = await _userRepository.GetByIdAsync(_tenantContext.TenantId, targetUserId, cancellationToken);
        if (target == null) throw new UnauthorizedAccessException("Access denied.");

        var canonical = RoleTemplates.Resolve(roleName);
        await _userRepository.AssignOrganizationRoleAsync(_tenantContext.TenantId, _userContext.OrganizationId!.Value, targetUserId, canonical, cancellationToken);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "Role.Assigned",
            EntityType = "User",
            EntityId = targetUserId,
            Details = new { Role = canonical, OrganizationId = _userContext.OrganizationId }
        }, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("user.manage")) throw new UnauthorizedAccessException("Requires user.manage permission.");
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetAllAsync(tenantId, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetTeachersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetUsersByRoleAsync(tenantId, "Teacher", cancellationToken);
    }

    public async Task<IEnumerable<User>> GetParentsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetUsersByRoleAsync(tenantId, "Parent", cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<User> CreateUserAsync(string email, string firstName, string lastName, string passwordHash, string? roleName = null, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("user.manage")) throw new UnauthorizedAccessException("Requires user.manage permission.");
        var tenantId = _tenantContext.TenantId;
        var normalizedEmail = email?.Trim().ToLowerInvariant();

        // 1. Check if user identity already exists across the platform
        User? existingUser = null;
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        }

        var targetUserId = existingUser?.Id ?? Guid.NewGuid();

        // Vet the requested role BEFORE creating anything, so a denied grant leaves no account behind.
        if (!string.IsNullOrEmpty(roleName))
        {
            await _authorizer.RequireCanGrantRoleAsync(roleName, targetUserId, cancellationToken);
        }

        User resultUser;
        var targetOrgId = _userContext.OrganizationId ?? tenantId;

        if (existingUser != null)
        {
            resultUser = existingUser;
            // Update names if the user had empty or draft placeholder names
            if (string.IsNullOrWhiteSpace(resultUser.FirstName) || resultUser.FirstName.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            {
                resultUser.FirstName = firstName;
                resultUser.LastName = lastName;
                resultUser.DisplayName = $"{firstName} {lastName}".Trim();
                await _userRepository.UpdateAsync(resultUser, cancellationToken);
            }
        }
        else
        {
            var user = new User
            {
                Id = targetUserId,
                TenantId = tenantId,
                Email = email ?? string.Empty,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = $"{firstName} {lastName}".Trim(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            resultUser = await _userRepository.AddAsync(user, passwordHash, cancellationToken);
        }

        if (!string.IsNullOrEmpty(roleName))
        {
            var resolvedRole = RoleTemplates.Resolve(roleName);
            await _userRepository.AssignOrganizationRoleAsync(tenantId, targetOrgId, resultUser.Id, resolvedRole, cancellationToken);
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "Role.Assigned",
                EntityType = "User",
                EntityId = resultUser.Id,
                Details = new { Role = resolvedRole, OrganizationId = targetOrgId }
            }, cancellationToken);
        }

        return resultUser;
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("user.manage") && !_userContext.HasPermission("staff.manage")) 
            throw new UnauthorizedAccessException("Requires user.manage permission.");
        
        var tenantId = _tenantContext.TenantId;
        if (user.TenantId != tenantId)
        {
            // Verify that the user has a valid scope or membership in the caller's tenant
            var existing = await _userRepository.GetByIdAsync(tenantId, user.Id, cancellationToken);
            if (existing == null)
            {
                throw new UnauthorizedAccessException("Cross-tenant update attempted.");
            }
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.DisplayName = $"{user.FirstName} {user.LastName}".Trim();
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("user.manage")) throw new UnauthorizedAccessException("Requires user.manage permission.");
        var tenantId = _tenantContext.TenantId;
        await _userRepository.DeleteAsync(tenantId, id, cancellationToken);
    }

    /// <summary>
    /// Promotes an existing teacher to Lead Teacher within an organization and branch context,
    /// configuring their classroom assignments and recording an audit trail.
    /// </summary>
    public async Task PromoteTeacherToLeadAsync(Guid targetUserId, Guid branchId, IReadOnlyList<Guid> classroomIds, CancellationToken cancellationToken = default)
    {
        if (targetUserId == _userContext.UserId)
            throw new UnauthorizedAccessException("Cannot promote yourself to Lead Teacher.");

        var canManage = _userContext.HasPermission(Permissions.StaffManage) || _userContext.HasPermission(Permissions.RoleAssign);
        if (!canManage)
            throw new UnauthorizedAccessException("Requires staff.manage or role.assign permission to promote teachers.");

        var tenantId = _tenantContext.TenantId;
        var organizationId = _userContext.OrganizationId ?? tenantId;

        // If caller is Branch Admin, verify they have authority over this specific branch
        if (_scopeRepo != null && !_userContext.HasPermission(Permissions.OrganizationManage))
        {
            var assignedBranchIds = await _scopeRepo.GetAssignedBranchIdsAsync(tenantId, organizationId, _userContext.UserId, cancellationToken);
            if (!assignedBranchIds.Contains(branchId))
            {
                throw new UnauthorizedAccessException("Branch Admin is not authorized to promote staff outside their assigned branch.");
            }
        }

        // Verify target user belongs to this tenant/organization
        var targetUser = await _userRepository.GetByIdAsync(tenantId, targetUserId, cancellationToken);
        if (targetUser == null)
            throw new InvalidOperationException("Target educator not found in the active organization context.");

        await _userRepository.PromoteTeacherToLeadAsync(tenantId, organizationId, branchId, targetUserId, _userContext.UserId, classroomIds, cancellationToken);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "Teacher.PromotedToLead",
            EntityType = "User",
            EntityId = targetUserId,
            Details = new
            {
                OrganizationId = organizationId,
                BranchId = branchId,
                ClassroomIds = classroomIds,
                PromotedBy = _userContext.UserId,
                NewRole = RoleTemplates.LeadTeacher
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Updates classroom assignments for a teacher or lead teacher within a specific branch.
    /// </summary>
    public async Task UpdateTeacherClassroomsAsync(Guid targetUserId, Guid branchId, IReadOnlyList<Guid> classroomIds, CancellationToken cancellationToken = default)
    {
        var canManage = _userContext.HasPermission(Permissions.StaffManage) || _userContext.HasPermission(Permissions.ClassroomManage);
        if (!canManage)
            throw new UnauthorizedAccessException("Requires staff.manage or classroom.manage permission to update classroom assignments.");

        var tenantId = _tenantContext.TenantId;
        var organizationId = _userContext.OrganizationId ?? tenantId;

        if (_scopeRepo != null && !_userContext.HasPermission(Permissions.OrganizationManage))
        {
            var assignedBranchIds = await _scopeRepo.GetAssignedBranchIdsAsync(tenantId, organizationId, _userContext.UserId, cancellationToken);
            if (!assignedBranchIds.Contains(branchId))
            {
                throw new UnauthorizedAccessException("Branch Admin is not authorized to manage staff outside their assigned branch.");
            }
        }

        var targetUser = await _userRepository.GetByIdAsync(tenantId, targetUserId, cancellationToken);
        if (targetUser == null)
            throw new InvalidOperationException("Target educator not found in the active organization context.");

        await _userRepository.UpdateTeacherClassroomsAsync(tenantId, organizationId, branchId, targetUserId, classroomIds, cancellationToken);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "Teacher.ClassroomsUpdated",
            EntityType = "User",
            EntityId = targetUserId,
            Details = new
            {
                OrganizationId = organizationId,
                BranchId = branchId,
                ClassroomIds = classroomIds,
                UpdatedBy = _userContext.UserId
            }
        }, cancellationToken);
    }
}

