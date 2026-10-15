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

    public UserService(IUserRepository userRepository, ITenantContext tenantContext, IUserContext userContext)
        : this(userRepository, tenantContext, userContext, new NullResourceAuthorizer(), new NullAuditService())
    {
    }

    public UserService(IUserRepository userRepository, ITenantContext tenantContext, IUserContext userContext,
        IResourceAuthorizer authorizer, IAuditService auditService)
    {
        _userRepository = userRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _authorizer = authorizer;
        _auditService = auditService;
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
        var newUserId = Guid.NewGuid();

        // Vet the requested role BEFORE creating anything, so a denied grant leaves no account behind.
        if (!string.IsNullOrEmpty(roleName))
        {
            await _authorizer.RequireCanGrantRoleAsync(roleName, newUserId, cancellationToken);
        }

        var user = new User
        {
            Id = newUserId,
            TenantId = tenantId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{firstName} {lastName}",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user, passwordHash, cancellationToken);

        if (!string.IsNullOrEmpty(roleName))
        {
            await _userRepository.AssignOrganizationRoleAsync(tenantId, _userContext.OrganizationId!.Value, createdUser.Id, RoleTemplates.Resolve(roleName), cancellationToken);
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "Role.Assigned",
                EntityType = "User",
                EntityId = createdUser.Id,
                Details = new { Role = RoleTemplates.Resolve(roleName), OrganizationId = _userContext.OrganizationId }
            }, cancellationToken);
        }

        return createdUser;
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("user.manage")) throw new UnauthorizedAccessException("Requires user.manage permission.");
        var tenantId = _tenantContext.TenantId;
        if (user.TenantId != tenantId) throw new UnauthorizedAccessException("Cross-tenant update attempted.");
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.DisplayName = $"{user.FirstName} {user.LastName}";
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("user.manage")) throw new UnauthorizedAccessException("Requires user.manage permission.");
        var tenantId = _tenantContext.TenantId;
        await _userRepository.DeleteAsync(tenantId, id, cancellationToken);
    }
}

