namespace EnjoyEveryday.Domain.Entities;

public class TeacherEducation
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? Diploma { get; set; }
    public string? Certificate { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? GraduationDate { get; set; }
    public string? Country { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string? Notes { get; set; }
}
