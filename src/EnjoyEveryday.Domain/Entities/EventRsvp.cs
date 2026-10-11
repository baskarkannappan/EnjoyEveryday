using System;

namespace EnjoyEveryday.Domain.Entities;

public class EventRsvp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid ChildId { get; set; }
    public Guid GuardianId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Attending, NotAttending
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
