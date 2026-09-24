namespace EnjoyEveryday.Domain.Entities;

public class ExperienceVersion
{
    public Guid Id { get; set; }
    public Guid ExperienceId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DnaPayload { get; set; } = "{}";
    public string ChangeReason { get; set; } = string.Empty;
    public Guid ModifiedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
