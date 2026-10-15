namespace EnjoyEveryday.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
    public Guid ClassroomId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Enrolled"; // Enrolled, Withdrawn, Graduated, etc.
    public string ScheduleType { get; set; } = "Full Time";
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

