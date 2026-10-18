using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Domain.Repositories;

public class PortfolioConsentDocumentRecord
{
    public Guid Id { get; set; }
    public string? Status { get; set; }
    public string? DocumentTitle { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public string DocumentType { get; set; } = string.Empty;
}

public class PortfolioChildProfileRecord
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ClassroomName { get; set; }
    public string? BranchName { get; set; }
}

public class DevelopmentAreaRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class PortfolioJourneyEntryRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset ObservationDate { get; set; }
    public string? JourneyType { get; set; }
    public string? Title { get; set; }
    public string? Observation { get; set; }
    public string? ChildVoice { get; set; }
    public string? TeacherReflection { get; set; }
    public bool IsMilestone { get; set; }
    public bool IsParentVisible { get; set; }
    public string? ParentVisibilityStatus { get; set; }
    public string TeacherName { get; set; } = string.Empty;
}

public class EntryDevelopmentAreaRecord
{
    public Guid JourneyEntryId { get; set; }
    public Guid DevelopmentAreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class EntryEvidenceRecord
{
    public Guid JourneyEntryId { get; set; }
    public string TextContent { get; set; } = string.Empty;
}

public class PortfolioMilestoneRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset MilestoneDate { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? EvidenceSummary { get; set; }
    public bool ParentVisible { get; set; }
    public string? Status { get; set; }
    public Guid? DevelopmentAreaId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
}

public class PortfolioExperienceParticipationRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset SessionDate { get; set; }
    public string? ExperienceTitle { get; set; }
    public string? Notes { get; set; }
    public string TeacherName { get; set; } = string.Empty;
}

public interface IChildDevelopmentPortfolioRepository
{
    Task<PortfolioConsentDocumentRecord?> GetLatestMediaConsentDocumentAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<List<string>> GetConsentChoicesAsync(Guid tenantId, Guid consentDocumentId, CancellationToken ct = default);
    Task<PortfolioChildProfileRecord?> GetChildProfileAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<List<DevelopmentAreaRecord>> GetDevelopmentAreasAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<PortfolioJourneyEntryRecord>> GetJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<List<EntryDevelopmentAreaRecord>> GetEntryDevelopmentAreasAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<List<EntryEvidenceRecord>> GetEntryEvidenceAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<List<PortfolioMilestoneRecord>> GetMilestonesAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<List<PortfolioExperienceParticipationRecord>> GetExperienceParticipationsAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
}
