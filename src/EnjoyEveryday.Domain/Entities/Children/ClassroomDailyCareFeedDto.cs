using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public class ClassroomDailyCareFeedDto
{
    public Guid BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public DateOnly Date { get; set; }
    public int TotalRecordedEventsToday { get; set; }
    public List<ClassroomChildCareStatusDto> ClassroomOverview { get; set; } = new();
    public List<ChildCareSupply> LowSupplies { get; set; } = new();
    public List<TeacherHandover> PendingHandovers { get; set; } = new();
    public List<TeacherHandover> ClassroomHandovers { get; set; } = new();
}

public class ClassroomChildCareStatusDto
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public bool IsCheckedIn { get; set; }
    public string AttendanceStatus { get; set; } = "Not Checked In";
    public TimeSpan? ArrivalTime { get; set; }

    public ChildCareEvent? LastDiaperEvent { get; set; }
    public ChildCareEvent? LastMealEvent { get; set; }
    public ChildNapSession? ActiveNapSession { get; set; }
    public ChildCareEvent? LastComfortMood { get; set; }
    public int TodayEventsCount { get; set; }
    public bool HasLowSupplies { get; set; }
    public string? ImportantCareAlert { get; set; }
}

public class ChildCarePackageDto
{
    public Guid ChildId { get; set; }
    public DateOnly Date { get; set; }
    public List<ChildTimelineEntryDto> ChildTimeline { get; set; } = new();
    public List<ChildCareSupply> ChildSupplies { get; set; } = new();
    public ChildCarePlan? CurrentPlan { get; set; }
}

public class ChildTimelineEntryDto
{
    public Guid? Id { get; set; }
    public DateTime EventTime { get; set; }
    public string TimeLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = "•";
    public string? StaffName { get; set; }
    public bool IsParentVisible { get; set; } = true;
    public bool IsCorrected { get; set; }
}

public class RecordBatchCareEventsRequest
{
    public List<Guid> ChildIds { get; set; } = new();
    public string EventType { get; set; } = string.Empty;
    public string DetailOption { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool ParentVisible { get; set; } = true;
}

public class BatchNapRequest
{
    public List<Guid> ChildIds { get; set; } = new();
    public Guid? ClassroomId { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
}
