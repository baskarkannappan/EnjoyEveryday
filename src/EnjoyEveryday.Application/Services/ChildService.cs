using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;

namespace EnjoyEveryday.Application.Services;

public class ChildService
{
    private readonly IChildRepository _childRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly IUserContext _userContext;
    private readonly IResourceAuthorizer _authorizer;

    public ChildService(IChildRepository childRepository, ITenantContext tenantContext, IAuditService auditService, IUserContext userContext)
        : this(childRepository, tenantContext, auditService, userContext, new NullResourceAuthorizer())
    {
    }

    public ChildService(IChildRepository childRepository, ITenantContext tenantContext, IAuditService auditService, IUserContext userContext, IResourceAuthorizer authorizer)
    {
        _childRepository = childRepository;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _userContext = userContext;
        _authorizer = authorizer;
    }

    public async Task<IEnumerable<Child>> GetChildrenAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var all = await _childRepository.GetAllAsync(tenantId, cancellationToken);
        // Per-child scope check: org admin sees the org, branch admin assigned branches, teachers assigned classrooms.
        return await _authorizer.FilterAccessibleChildrenAsync(all, Permissions.ChildView, cancellationToken);
    }

    public async Task<IEnumerable<Child>> GetChildrenByClassroomAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        await _authorizer.RequireClassroomAsync(classroomId, Permissions.ChildView, cancellationToken);
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetByClassroomIdAsync(tenantId, classroomId, cancellationToken);
    }

    public async Task<Child?> GetChildByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Unknown id, other tenant and out-of-scope all fail identically (no existence disclosure).
        await _authorizer.RequireChildAsync(id, Permissions.ChildView, cancellationToken);
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<Child> CreateChildAsync(string firstName, string lastName, DateTime? dateOfBirth, Guid? classroomId, string? photoUrl = null, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("child.manage")) throw new UnauthorizedAccessException("Requires child.manage permission.");
        if (classroomId.HasValue)
            await _authorizer.RequireClassroomAsync(classroomId.Value, Permissions.ChildManage, cancellationToken);
        else
            await _authorizer.RequireOrganizationPermissionAsync(Permissions.ChildManage, cancellationToken);
        var tenantId = _tenantContext.TenantId;
        var child = new Child
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            ClassroomId = classroomId,
            PhotoUrl = photoUrl,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _childRepository.AddAsync(child, cancellationToken);
    }

    public async Task UpdateChildAsync(Child child, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("child.manage")) throw new UnauthorizedAccessException("Requires child.manage permission.");
        // Body IDs are untrusted: scope is checked against the STORED child, not the submitted object.
        await _authorizer.RequireChildAsync(child.Id, Permissions.ChildManage, cancellationToken);
        if (child.TenantId != _tenantContext.TenantId) throw new UnauthorizedAccessException("Access denied.");
        var existingChild = await _childRepository.GetByIdAsync(_tenantContext.TenantId, child.Id, cancellationToken);
        if (existingChild != null && existingChild.ClassroomId != child.ClassroomId)
        {
            // Moving a child requires enrollment authority over the destination as well.
            if (child.ClassroomId.HasValue)
                await _authorizer.RequireClassroomAsync(child.ClassroomId.Value, Permissions.ChildEnrollmentManage, cancellationToken);
            else
                await _authorizer.RequireOrganizationPermissionAsync(Permissions.ChildEnrollmentManage, cancellationToken);
        }
        if (existingChild != null && existingChild.ClassroomId != child.ClassroomId)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                Action = "ChildTransferred",
                EntityType = "Child",
                EntityId = child.Id,
                Details = new 
                { 
                    PreviousClassroomId = existingChild.ClassroomId, 
                    NewClassroomId = child.ClassroomId,
                    TransferDate = DateTimeOffset.UtcNow,
                    Reason = "Transferred via UI"
                }
            }, cancellationToken);
        }

        child.UpdatedAt = DateTimeOffset.UtcNow;
        await _childRepository.UpdateAsync(child, cancellationToken);
    }

    public async Task DeleteChildAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("child.manage")) throw new UnauthorizedAccessException("Requires child.manage permission.");
        await _authorizer.RequireChildAsync(id, Permissions.ChildEnrollmentManage, cancellationToken);
        var tenantId = _tenantContext.TenantId;
        await _childRepository.DeleteAsync(tenantId, id, cancellationToken);
    }
}
