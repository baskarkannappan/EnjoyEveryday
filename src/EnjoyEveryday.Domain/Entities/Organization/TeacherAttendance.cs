namespace EnjoyEveryday.Domain.Entities;
public class TeacherAttendance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid TeacherId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? ClockInTime { get; set; }
    public TimeSpan? ClockOutTime { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}