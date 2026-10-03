namespace EnjoyEveryday.UI.Shared.Components;

public class CalendarItem
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}
