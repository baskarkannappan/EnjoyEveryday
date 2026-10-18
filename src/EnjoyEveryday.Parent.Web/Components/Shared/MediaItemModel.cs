namespace EnjoyEveryday.Parent.Web.Components.Shared;

public class MediaItemModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public string Category { get; set; } = "Memories"; // Memories, Celebration, Journey, Activity
    public string? ChildName { get; set; }
    public bool IsVideo { get; set; }
}
