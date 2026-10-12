namespace EnjoyEveryday.UI.Shared.Components;

public class FeedbackItem
{
    public Guid Id { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Rating { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

