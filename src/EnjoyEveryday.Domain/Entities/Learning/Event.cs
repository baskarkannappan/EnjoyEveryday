using System;

namespace EnjoyEveryday.Domain.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? EventTypeId { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Location { get; set; }
    public Guid? OrganizerId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Planned, Approved, Published, InProgress, Completed, Cancelled
    public string Scope { get; set; } = "Classroom"; // Organization, Branch, Classroom
    public Guid? BranchId { get; set; }
    public bool IsOrganizationWide 
    { 
        get => Scope == "Organization"; 
        set { if (value) Scope = "Organization"; } 
    }
    public string[] PhotoUrls { get; set; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
