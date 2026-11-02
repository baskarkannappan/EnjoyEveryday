using System;

namespace EnjoyEveryday.Domain.Entities.Attendance;

public class AttendanceAttentionAlertDto
{
    public string AlertType { get; set; } = string.Empty;
    public string Icon { get; set; } = "◷";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string ActionText { get; set; } = "Review";
    public string ActionUrl { get; set; } = string.Empty;
    public string Severity { get; set; } = "amber";
}
