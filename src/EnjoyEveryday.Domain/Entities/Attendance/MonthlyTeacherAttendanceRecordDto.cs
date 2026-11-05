using System;

namespace EnjoyEveryday.Domain.Entities.Attendance;

public class MonthlyTeacherAttendanceRecordDto
{
    public Guid TeacherId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public Guid? AttendanceId { get; set; }
    public DateTime? AttendanceDate { get; set; }
    public int? DayOfMonth { get; set; }
    public string? Status { get; set; }
    public TimeSpan? ClockInTime { get; set; }
    public TimeSpan? ClockOutTime { get; set; }
    public string? Notes { get; set; }
}

