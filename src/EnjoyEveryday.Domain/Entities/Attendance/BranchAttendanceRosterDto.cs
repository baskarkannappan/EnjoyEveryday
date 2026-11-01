using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities.Attendance;

public class BranchAttendanceRosterDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<ClassroomAttendanceRosterDto> Classrooms { get; set; } = new();
    public List<PersonAttendanceRosterDto> People { get; set; } = new();
}

public class ClassroomAttendanceRosterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChildrenRatio { get; set; } = string.Empty;
    public string TeachersRatio { get; set; } = string.Empty;
    public string Pending { get; set; } = "0";
    public string Status { get; set; } = "Marked";
}

public class PersonAttendanceRosterDto
{
    public Guid Id { get; set; }
    public Guid? AttendanceId { get; set; }
    public Guid ClassroomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Status { get; set; } = "Not marked";
    public string In { get; set; } = "—";
    public string Out { get; set; } = "—";
    public string? PickupPersonName { get; set; }
    public string? PickupPersonRelationship { get; set; }
    public string? PickupVerificationMethod { get; set; }
    public string? PickupNotes { get; set; }
    public string? Notes { get; set; }
    public string Type { get; set; } = "children";
}

public class BulkMarkAttendanceItem
{
    public Guid Id { get; set; }
    public Guid? AttendanceId { get; set; }
    public Guid ClassroomId { get; set; }
    public string Status { get; set; } = "Present";
    public string In { get; set; } = "—";
    public string Type { get; set; } = "children";
}

public class BulkMarkAttendanceRequest
{
    public Guid BranchId { get; set; }
    public DateTime Date { get; set; }
    public List<BulkMarkAttendanceItem> Items { get; set; } = new();
}
