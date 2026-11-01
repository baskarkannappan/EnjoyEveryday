using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities.Learning;

public class PlanningCalendarSummaryDto
{
    public Guid ClassroomId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<ExperienceSchedule> Schedules { get; set; } = new();
    public List<Experience> Experiences { get; set; } = new();
    public List<EventSummaryDto> Events { get; set; } = new();

    // Rhythm Mix
    public int ExploreCount { get; set; }
    public int CreateCount { get; set; }
    public int TogetherCount { get; set; }
    public int MoveCount { get; set; }
    public int TotalRhythmCount => (ExploreCount + CreateCount + TogetherCount + MoveCount) > 0 
        ? (ExploreCount + CreateCount + TogetherCount + MoveCount) 
        : 1;

    // Management Focus & Goals
    public string TopGoal { get; set; } = "Community Building";
    public List<string> TopGoalsList { get; set; } = new();

    // Materials & Suggestions
    public string CommonMaterial { get; set; } = "natural materials";
    public List<string> AiSuggestionsList { get; set; } = new();

    // Adaptations
    public List<string> AdaptationsList { get; set; } = new();
}

public class EventSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string Status { get; set; } = "Planned";
    public string Scope { get; set; } = "Classroom";
    public Guid? BranchId { get; set; }
    public bool IsOrganizationWide { get; set; }
    public List<Guid> AssignedClassroomIds { get; set; } = new();
}

public class OrderMissingMaterialsRequest
{
    public Guid ScheduleId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? DeliveryAddress { get; set; }
}

public class OrderMissingMaterialsResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OrderNumber { get; set; }
    public object? UpdatedReadiness { get; set; }
}

