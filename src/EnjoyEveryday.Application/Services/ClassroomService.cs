using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ClassroomService
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly ITenantContext _tenantContext;

    public ClassroomService(IClassroomRepository classroomRepository, ITenantContext tenantContext)
    {
        _classroomRepository = classroomRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Classroom>> GetClassroomsAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _classroomRepository.GetByBranchIdAsync(tenantId, branchId, cancellationToken);
    }

    public async Task<Classroom?> GetClassroomByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _classroomRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<Classroom> CreateClassroomAsync(Guid branchId, string name, string? ageGroup, int? capacity, string environment, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            Name = name,
            AgeGroup = ageGroup,
            Capacity = capacity,
            Environment = environment,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _classroomRepository.AddAsync(classroom, cancellationToken);
    }

    public async Task UpdateClassroomAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (classroom.TenantId != tenantId)
            throw new UnauthorizedAccessException("Cross-tenant update attempted.");

        classroom.UpdatedAt = DateTimeOffset.UtcNow;
        await _classroomRepository.UpdateAsync(classroom, cancellationToken);
    }

    public async Task DeleteClassroomAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        await _classroomRepository.DeleteAsync(tenantId, id, cancellationToken);
    }
}
