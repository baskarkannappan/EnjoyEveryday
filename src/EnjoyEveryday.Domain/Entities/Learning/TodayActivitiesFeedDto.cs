using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities.Learning;

public class TodayActivitiesFeedDto
{
    public DateOnly Date { get; set; }
    public int TotalScheduled { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int UpcomingCount { get; set; }
    public List<TodayActivityItemDto> Activities { get; set; } = new();
}

public class TodayActivityItemDto
{
    public Guid ScheduleId { get; set; }
    public Guid ExperienceId { get; set; }
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeOfDay { get; set; } = string.Empty;
    public TimeSpan? PlannedStartTime { get; set; }
    public TimeSpan? PlannedEndTime { get; set; }
    public string TimeRangeDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public string? PreparationNotes { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public Guid? PrimaryTeacherId { get; set; }

    public bool IsRunning => Status == "Started" || Status == "Active" || Status == "InProgress";
    public bool IsCompleted => Status == "Completed" || Status == "Finished";
    public bool IsPending => !IsRunning && !IsCompleted;

    public TimeSpan GetSortOrderTime()
    {
        if (PlannedStartTime.HasValue) return PlannedStartTime.Value;
        return TimeOfDay?.ToLowerInvariant() switch
        {
            "morning" => new TimeSpan(9, 0, 0),
            "mid-day" => new TimeSpan(11, 30, 0),
            "afternoon" => new TimeSpan(14, 0, 0),
            _ => new TimeSpan(16, 0, 0)
        };
    }
}
