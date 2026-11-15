using System;

namespace EnjoyEveryday.Domain.Entities;

public class BranchDailyStatusDto
{
    public Guid BranchId { get; set; }
    public DateOnly OperationDate { get; set; }
    public string Status { get; set; } = "Closed"; // "Closed", "Open"
    public DateTimeOffset? OpenedAt { get; set; }
    public string? OpenedByName { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosedByName { get; set; }
    public int TotalClassrooms { get; set; }
    public int OpenClassrooms { get; set; }
    public int ClosedClassrooms { get; set; }
    public int PendingChildrenCount { get; set; }

    public bool IsOpen => string.Equals(Status, "Open", StringComparison.OrdinalIgnoreCase);
}

public class ClassroomDailyStatusDto
{
    public Guid ClassroomId { get; set; }
    public Guid BranchId { get; set; }
    public DateOnly OperationDate { get; set; }
    public string Status { get; set; } = "Closed"; // "Closed", "Open"
    public DateTimeOffset? OpenedAt { get; set; }
    public string? OpenedByName { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosedByName { get; set; }
    public string BranchStatus { get; set; } = "Closed";
    public int ActiveChildrenCount { get; set; }
    public int ActiveActivitiesCount { get; set; }

    public bool IsOpen => string.Equals(Status, "Open", StringComparison.OrdinalIgnoreCase);
    public bool IsBranchOpen => string.Equals(BranchStatus, "Open", StringComparison.OrdinalIgnoreCase);
}

public class DailyOperationExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AffectedChildrenCount { get; set; }
    public int AffectedActivitiesCount { get; set; }
    public int OpenClassroomsCount { get; set; }
    public int AffectedTeachersCount { get; set; }
}

