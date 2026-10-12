using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public class ParentAuthorizedChild
{
    public Guid ChildId { get; set; }
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? OrganizationName { get; set; }
    public string? PhotoUrl { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public interface IParentRepository
{
    Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasChildAccessAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken cancellationToken = default);
    Task<ChildAttendance?> GetChildAttendanceForDateAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildJourneyEntry>> GetApprovedJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Event>> GetApprovedEventsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<Person?> GetPersonByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task UpdatePersonContactAsync(Guid tenantId, Guid personId, string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken cancellationToken = default);
}
