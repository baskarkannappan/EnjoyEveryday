using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IChildJourneyRepository
{
    Task<ChildJourneyEntry?> GetJourneyEntryByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildJourneyEntry>> GetJourneyEntriesForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task CreateJourneyEntryAsync(ChildJourneyEntry entry, CancellationToken cancellationToken = default);
    Task UpdateJourneyEntryAsync(ChildJourneyEntry entry, CancellationToken cancellationToken = default);
    Task DeleteJourneyEntryAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<DevelopmentArea>> GetAllDevelopmentAreasAsync(Guid? tenantId, CancellationToken cancellationToken = default);
    
    Task AddJourneyDevelopmentsAsync(Guid journeyEntryId, IEnumerable<Guid> developmentAreaIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetDevelopmentAreasForJourneyEntryAsync(Guid journeyEntryId, CancellationToken cancellationToken = default);

    Task CreateJourneyEvidenceAsync(ChildJourneyEvidence evidence, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildJourneyEvidence>> GetEvidenceForJourneyEntryAsync(Guid journeyEntryId, CancellationToken cancellationToken = default);

    Task CreateMilestoneAsync(ChildMilestone milestone, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildMilestone>> GetMilestonesForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
}
