using System;

namespace EnjoyEveryday.Domain.Entities;

public class AuthorizedPickup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
    public Guid? PersonId { get; set; }
    public string? Relationship { get; set; }
    public string? PickupCode { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
