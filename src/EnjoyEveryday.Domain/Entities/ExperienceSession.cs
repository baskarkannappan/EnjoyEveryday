namespace EnjoyEveryday.Domain.Entities;

public class ExperienceSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid? ExperienceScheduleId { get; set; }
    public Guid ClassroomId { get; set; }
    public DateTime SessionDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string Status { get; set; } = "Planned";
    public Guid? PrimaryTeacherId { get; set; }
    public string? ActualNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
