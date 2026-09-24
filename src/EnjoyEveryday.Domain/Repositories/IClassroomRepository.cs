using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IClassroomRepository
{
    Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
    Task<Classroom> AddAsync(Classroom classroom, CancellationToken cancellationToken = default);
    Task UpdateAsync(Classroom classroom, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
