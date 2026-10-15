namespace EnjoyEveryday.Domain.Entities;

public class TeacherSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ShiftType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
