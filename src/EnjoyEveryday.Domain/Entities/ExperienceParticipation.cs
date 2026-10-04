namespace EnjoyEveryday.Domain.Entities;

public class ExperienceParticipation
{
    public Guid Id { get; set; }
    public Guid ExperienceSessionId { get; set; }
    public Guid ChildId { get; set; }
    public string ParticipationStatus { get; set; } = "Participated";
    public string? ParticipationNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
