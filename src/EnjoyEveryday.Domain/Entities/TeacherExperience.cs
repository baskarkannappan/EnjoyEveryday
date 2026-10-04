namespace EnjoyEveryday.Domain.Entities;
public class TeacherExperience
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AgeGroup { get; set; }
    public string? Description { get; set; }
    public string? ReferenceInformation { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}
