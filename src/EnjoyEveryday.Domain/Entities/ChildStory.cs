namespace EnjoyEveryday.Domain.Entities;

public class ChildStory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid? ExperienceScheduleId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Approved, Published
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
