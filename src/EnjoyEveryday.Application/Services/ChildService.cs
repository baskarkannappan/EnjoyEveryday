using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ChildService
{
    private readonly IChildRepository _childRepository;
    private readonly ITenantContext _tenantContext;

    public ChildService(IChildRepository childRepository, ITenantContext tenantContext)
    {
        _childRepository = childRepository;
        _tenantContext = tenantContext;
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

    public async Task<Child> CreateChildAsync(string firstName, string lastName, DateTime? dateOfBirth, Guid? classroomId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var child = new Child
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            ClassroomId = classroomId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _childRepository.AddAsync(child, cancellationToken);
    }

    public async Task UpdateChildAsync(Child child, CancellationToken cancellationToken = default)
    {
        child.UpdatedAt = DateTimeOffset.UtcNow;
        await _childRepository.UpdateAsync(child, cancellationToken);
    }

    public async Task DeleteChildAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        await _childRepository.DeleteAsync(tenantId, id, cancellationToken);
    }
}
