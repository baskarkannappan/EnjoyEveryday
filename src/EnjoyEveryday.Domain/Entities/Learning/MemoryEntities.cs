namespace EnjoyEveryday.Domain.Entities;

public class MemoryEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset EventDate { get; set; }
    public string EventType { get; set; } = string.Empty; // "Observation", "Feedback", "Session", "Milestone", "Celebration", "DailyCare"
    public string Category { get; set; } = "Observations"; // "Learning Experiences", "Observations", "Milestones", "Celebrations", "Daily Care"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public Guid SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty; // "child_stories", "child_journey_entries", "experience_participations", "child_milestones", "events", "child_daily_care_entries"

    public Guid? ChildId { get; set; }
    public string? ChildName { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public Guid? ExperienceId { get; set; }
    public string? ExperienceTitle { get; set; }
    public string? MediaUrl { get; set; }
    public bool IsParentVisible { get; set; } = true;
    public string? Status { get; set; }
    public string? CategoryIcon { get; set; }
}

public class MemoryQueryParameters
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? Category { get; set; }
    public string? SearchQuery { get; set; }
    public int Limit { get; set; } = 50;
    public int Offset { get; set; } = 0;
}
