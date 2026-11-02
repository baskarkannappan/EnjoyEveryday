using System;

namespace EnjoyEveryday.Domain.Entities.Attendance;

public class ClassroomAttendanceSummaryDto
{
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int ExpectedChildren { get; set; }
    public int PresentChildren { get; set; }
    public int ExpectedTeachers { get; set; }
    public int PresentTeachers { get; set; }
    public int UnmarkedChildren { get; set; }
    public string StatusText { get; set; } = "Marked";
    public string StatusCss { get; set; } = "green";
    public string StatusCategory { get; set; } = "all";
}
