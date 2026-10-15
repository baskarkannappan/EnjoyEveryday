using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IChildRepository
{
    Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default);
    Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default);
    Task UpdateAsync(Child child, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
