using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IExperienceRepository
{
    Task<Experience?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default);
    Task UpdateAsync(Experience experience, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExperienceVersion>> GetVersionsAsync(Guid experienceId, CancellationToken cancellationToken = default);
}
