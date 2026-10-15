using System;

namespace EnjoyEveryday.Domain.Entities;

public class ExperienceSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid ClassroomId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string TimeOfDay { get; set; } = string.Empty; // Morning, Afternoon, etc.
    public string Status { get; set; } = "Scheduled"; // Draft, Planned, Confirmed, Started, Completed, Cancelled
    public Guid? PrimaryTeacherId { get; set; }
    public TimeSpan? PlannedStartTime { get; set; }
    public TimeSpan? PlannedEndTime { get; set; }
    public string? PreparationNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
