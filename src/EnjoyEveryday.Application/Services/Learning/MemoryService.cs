using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class MemoryService : IMemoryService
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;

    public MemoryService(IMemoryRepository memoryRepository, ITenantContext tenantContext, IUserContext? userContext = null)
    {
        _memoryRepository = memoryRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    public async Task<IEnumerable<MemoryEvent>> GetChildMemoriesAsync(Guid childId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        bool isParent = _userContext?.Roles?.Contains("Parent") ?? false;
        return await _memoryRepository.GetChildMemoriesAsync(_tenantContext.TenantId, childId, isParent, parameters, cancellationToken);
    }

    public async Task<IEnumerable<MemoryEvent>> GetClassroomMemoriesAsync(Guid classroomId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        bool isParent = _userContext?.Roles?.Contains("Parent") ?? false;
        return await _memoryRepository.GetClassroomMemoriesAsync(_tenantContext.TenantId, classroomId, isParent, parameters, cancellationToken);
    }

    public async Task<IEnumerable<MemoryEvent>> GetTeacherMemoriesAsync(Guid teacherId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        bool isParent = _userContext?.Roles?.Contains("Parent") ?? false;
        return await _memoryRepository.GetTeacherMemoriesAsync(_tenantContext.TenantId, teacherId, isParent, parameters, cancellationToken);
    }

    public async Task<IEnumerable<MemoryEvent>> GetExperienceMemoriesAsync(Guid experienceId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        bool isParent = _userContext?.Roles?.Contains("Parent") ?? false;
        return await _memoryRepository.GetExperienceMemoriesAsync(_tenantContext.TenantId, experienceId, isParent, parameters, cancellationToken);
    }
}
