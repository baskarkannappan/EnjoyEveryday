namespace EnjoyEveryday.Domain.Entities;

public class ChildJourneyEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JourneyEntryId { get; set; }
    public string EvidenceType { get; set; } = string.Empty; // Photo, Video, Audio, TeacherNote, ChildQuote
    public Guid? MediaId { get; set; }
    public string? TextContent { get; set; }
    public DateTimeOffset? CapturedAt { get; set; }
    public Guid? CapturedBy { get; set; }
    public string Visibility { get; set; } = "InternalOnly"; // InternalOnly, Classroom, Parent, Public
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
