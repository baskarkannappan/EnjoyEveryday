using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IMemoryRepository
{
    Task<IEnumerable<MemoryEvent>> GetChildMemoriesAsync(Guid tenantId, Guid childId, bool isParent, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemoryEvent>> GetClassroomMemoriesAsync(Guid tenantId, Guid classroomId, bool isParent, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemoryEvent>> GetTeacherMemoriesAsync(Guid tenantId, Guid teacherId, bool isParent, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemoryEvent>> GetExperienceMemoriesAsync(Guid tenantId, Guid experienceId, bool isParent, MemoryQueryParameters parameters, CancellationToken cancellationToken = default);
}
