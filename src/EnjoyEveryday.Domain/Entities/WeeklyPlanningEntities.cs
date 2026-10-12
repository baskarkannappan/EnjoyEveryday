using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public class WeeklyPlanningDraftRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public string? FocusAreas { get; set; }
    public string IndoorOutdoorPreference { get; set; } = "Balanced"; // Indoor, Outdoor, Balanced
    public string? MaterialsNotes { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Saved, Approved, Cancelled
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<WeeklyPlanningDraftItem> Items { get; set; } = new();
}

public class WeeklyPlanningDraftItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid TenantId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.
    public string TimeOfDay { get; set; } = "Morning"; // Morning, Afternoon
    public Guid ExperienceId { get; set; }
    public string ExperienceTitle { get; set; } = string.Empty;
    public string? LearningGoal { get; set; }
    public string GroupFormat { get; set; } = "Small Group"; // Individual, Small Group, Whole Class
    public int EstimatedDurationMinutes { get; set; } = 25;
    public string? RequiredMaterials { get; set; }
    public string? Rationale { get; set; }
    public string ValidationStatus { get; set; } = "Valid"; // Valid, Warning, Conflict
    public string? ValidationNotes { get; set; }
    public Guid? ResultingScheduleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class GenerateWeeklyPlanInput
{
    public Guid ClassroomId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string? FocusAreas { get; set; }
    public string IndoorOutdoorPreference { get; set; } = "Balanced";
    public string? AvailableMaterialsNotes { get; set; }
}

public class SubmitWeeklyPlanInput
{
    public Guid DraftRequestId { get; set; }
    public List<WeeklyPlanningDraftItem> ConfirmedItems { get; set; } = new();
    public bool AutoConfirmSchedules { get; set; } = false;
}
