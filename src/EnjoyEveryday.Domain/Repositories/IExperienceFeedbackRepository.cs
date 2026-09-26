using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IExperienceFeedbackRepository
{
    Task<ExperienceFeedback?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExperienceFeedback>> GetByScheduleIdAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExperienceFeedback>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ExperienceFeedback> AddAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
