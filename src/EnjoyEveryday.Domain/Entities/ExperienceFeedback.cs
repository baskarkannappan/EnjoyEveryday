namespace EnjoyEveryday.Domain.Entities;

public class ExperienceFeedback
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExperienceScheduleId { get; set; }
    public Guid TeacherId { get; set; }
    
    // Feedback data
    public string Rating { get; set; } = string.Empty; // e.g. "Loved it", "Good", "Difficult", "Didn't work"
    public string? Notes { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
}
