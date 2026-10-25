namespace EnjoyEveryday.UI.Shared.Components.RhythmHistory;

public class HistoricalRhythmItem
{
    public bool IsEvent { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string TimeDisplay { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Scope { get; set; } = "Classroom";
    public string ScopeDisplay { get; set; } = "";
    public string Status { get; set; } = "";
    public Guid? ClassroomId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ScheduleId { get; set; }
    public string[] PhotoUrls { get; set; } = Array.Empty<string>();
    public bool EditingPhotos { get; set; } = false;
}
