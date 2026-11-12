using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public class ChildGuardianDto
{
    public Guid PersonId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Relationship { get; set; } = "Parent";
    public bool IsPrimary { get; set; }
    public bool CanPickup { get; set; }
    public bool HasPortalAccess { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? VerifiedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public interface IChildRepository
{
    Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default) => GetAllAsync(tenantId, cancellationToken);
    Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default);
    Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default);
    Task UpdateAsync(Child child, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task SyncParentGuardianAsync(Guid tenantId, Guid childId, string parentEmail, string parentName, string relationship, string phone, CancellationToken cancellationToken = default) => Task.CompletedTask;
    Task<IEnumerable<ChildGuardianDto>> GetChildGuardiansAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ChildGuardianDto>());
    Task RevokeChildGuardianAsync(Guid tenantId, Guid childId, Guid userId, Guid? revokedBy = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
