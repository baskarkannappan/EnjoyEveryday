using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Audit;

namespace EnjoyEveryday.Application.Services;

public class ChildService
{
    private readonly IChildRepository _childRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly IUserContext _userContext;

    public ChildService(IChildRepository childRepository, ITenantContext tenantContext, IAuditService auditService, IUserContext userContext)
    {
        _childRepository = childRepository;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _userContext = userContext;
    }

    public async Task<IEnumerable<Child>> GetChildrenAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetAllAsync(tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Child>> GetChildrenByClassroomAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetByClassroomIdAsync(tenantId, classroomId, cancellationToken);
    }

    public async Task<Child?> GetChildByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _childRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<Child> CreateChildAsync(string firstName, string lastName, DateTime? dateOfBirth, Guid? classroomId, string? photoUrl = null, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("child.manage")) throw new UnauthorizedAccessException("Requires child.manage permission.");
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
        var existingChild = await _childRepository.GetByIdAsync(_tenantContext.TenantId, child.Id, cancellationToken);
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
        var tenantId = _tenantContext.TenantId;
        await _childRepository.DeleteAsync(tenantId, id, cancellationToken);
    }
}
