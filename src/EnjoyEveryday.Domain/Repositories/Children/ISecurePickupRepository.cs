using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public class ChildPickupHeaderData
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ClassroomName { get; set; }
    public string? BranchName { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? BranchId { get; set; }
}

public class CustodyRestrictionRecord
{
    public Guid Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string RestrictionType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}

public class AuthorizedPickupRecord
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Guid? PersonId { get; set; }
    public string? Relationship { get; set; }
    public string? PickupCode { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class FamilyPickupRelationshipRecord
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Guid PersonId { get; set; }
    public string? Relationship { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEmergency { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
}

public interface ISecurePickupRepository
{
    Task<ChildPickupHeaderData?> GetChildPickupHeaderAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<IReadOnlyList<CustodyRestrictionRecord>> GetActiveCustodyRestrictionsAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<IReadOnlyList<AuthorizedPickupRecord>> GetAuthorizedPickupsAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyPickupRelationshipRecord>> GetFamilyPickupRelationshipsAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<ChildAttendance?> GetTodayAttendanceAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken ct = default);
    Task AddAuthorizedPickupAsync(AuthorizedPickup pickup, CancellationToken ct = default);
    Task<bool> AuthorizedPickupExistsAsync(Guid tenantId, Guid pickupId, CancellationToken ct = default);
}
