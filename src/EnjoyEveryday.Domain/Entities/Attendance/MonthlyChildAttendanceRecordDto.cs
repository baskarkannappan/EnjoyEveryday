using System;

namespace EnjoyEveryday.Domain.Entities.Attendance;

public class MonthlyChildAttendanceRecordDto
{
    public Guid ChildId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public Guid? AttendanceId { get; set; }
    public DateTime? AttendanceDate { get; set; }
    public int? DayOfMonth { get; set; }
    public string? Status { get; set; }
    public TimeSpan? ArrivalTime { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    public string? Notes { get; set; }
}

