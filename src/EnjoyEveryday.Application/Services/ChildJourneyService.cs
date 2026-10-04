using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

using EnjoyEveryday.Shared.Audit;

namespace EnjoyEveryday.Application.Services;

public interface IChildJourneyService
{
    Task<ChildJourneyEntry?> GetJourneyEntryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildJourneyEntry>> GetChildJourneyAsync(Guid childId, CancellationToken cancellationToken = default);
    Task CreateJourneyEntryAsync(ChildJourneyEntry entry, IEnumerable<Guid> developmentAreaIds, CancellationToken cancellationToken = default);
    Task UpdateJourneyEntryAsync(ChildJourneyEntry entry, IEnumerable<Guid> developmentAreaIds, CancellationToken cancellationToken = default);
    Task DeleteJourneyEntryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<DevelopmentArea>> GetDevelopmentAreasAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetDevelopmentAreasForEntryAsync(Guid entryId, CancellationToken cancellationToken = default);

    Task CreateJourneyEvidenceAsync(ChildJourneyEvidence evidence, CancellationToken cancellationToken = default);

    Task CreateMilestoneAsync(ChildMilestone milestone, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildMilestone>> GetMilestonesAsync(Guid childId, CancellationToken cancellationToken = default);
}

public class ChildJourneyService : IChildJourneyService
{
    private readonly IChildJourneyRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;

    public ChildJourneyService(IChildJourneyRepository repository, ITenantContext tenantContext, IAuditService auditService)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _auditService = auditService;
    }

    public async Task<ChildJourneyEntry?> GetJourneyEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetJourneyEntryByIdAsync(_tenantContext.TenantId, id, cancellationToken);
    }

    public async Task<IEnumerable<ChildJourneyEntry>> GetChildJourneyAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetJourneyEntriesForChildAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task CreateJourneyEntryAsync(ChildJourneyEntry entry, IEnumerable<Guid> developmentAreaIds, CancellationToken cancellationToken = default)
    {
        entry.TenantId = _tenantContext.TenantId;
        if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
        
        await _repository.CreateJourneyEntryAsync(entry, cancellationToken);
        
        if (developmentAreaIds != null && developmentAreaIds.Any())
        {
            await _repository.AddJourneyDevelopmentsAsync(entry.Id, developmentAreaIds, cancellationToken);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = entry.TeacherId,
            Action = "JourneyCreated",
            EntityType = "ChildJourneyEntry",
            EntityId = entry.Id,
            Details = new { entry.ChildId, entry.JourneyType }
        }, cancellationToken);
    }

    public async Task UpdateJourneyEntryAsync(ChildJourneyEntry entry, IEnumerable<Guid> developmentAreaIds, CancellationToken cancellationToken = default)
    {
        entry.TenantId = _tenantContext.TenantId;
        
        await _repository.UpdateJourneyEntryAsync(entry, cancellationToken);
        
        if (developmentAreaIds != null)
        {
            await _repository.AddJourneyDevelopmentsAsync(entry.Id, developmentAreaIds, cancellationToken);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = entry.TeacherId, // Might be null if updated by someone else, but okay for MVP
            Action = "JourneyUpdated",
            EntityType = "ChildJourneyEntry",
            EntityId = entry.Id,
            Details = new { entry.ChildId, entry.JourneyType }
        }, cancellationToken);
    }

    public async Task DeleteJourneyEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteJourneyEntryAsync(_tenantContext.TenantId, id, cancellationToken);
    }

    public async Task<IEnumerable<DevelopmentArea>> GetDevelopmentAreasAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllDevelopmentAreasAsync(_tenantContext.TenantId, cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetDevelopmentAreasForEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetDevelopmentAreasForJourneyEntryAsync(entryId, cancellationToken);
    }

    public async Task CreateJourneyEvidenceAsync(ChildJourneyEvidence evidence, CancellationToken cancellationToken = default)
    {
        if (evidence.Id == Guid.Empty) evidence.Id = Guid.NewGuid();
        await _repository.CreateJourneyEvidenceAsync(evidence, cancellationToken);
    }

    public async Task CreateMilestoneAsync(ChildMilestone milestone, CancellationToken cancellationToken = default)
    {
        milestone.TenantId = _tenantContext.TenantId;
        if (milestone.Id == Guid.Empty) milestone.Id = Guid.NewGuid();
        
        await _repository.CreateMilestoneAsync(milestone, cancellationToken);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            Action = "MilestoneCaptured",
            EntityType = "ChildMilestone",
            EntityId = milestone.Id,
            Details = new { milestone.ChildId, Category = milestone.Title }
        }, cancellationToken);
    }

    public async Task<IEnumerable<ChildMilestone>> GetMilestonesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetMilestonesForChildAsync(_tenantContext.TenantId, childId, cancellationToken);
    }
}
