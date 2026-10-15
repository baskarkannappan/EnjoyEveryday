using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IChildStoryRepository
{
    Task<IEnumerable<ChildStory>> GetByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildInterest>> GetInterestsByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<ChildStory> AddStoryAsync(ChildStory story, CancellationToken cancellationToken = default);
    Task<ChildInterest> AddInterestAsync(ChildInterest interest, CancellationToken cancellationToken = default);
    Task UpdateStoryStatusAsync(Guid tenantId, Guid id, string status, CancellationToken cancellationToken = default);
}
