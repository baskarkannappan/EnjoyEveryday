namespace EnjoyEveryday.Domain.Entities;
public class TeacherQualification
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string QualificationType { get; set; } = string.Empty;
    public string QualificationName { get; set; } = string.Empty;
    public string IssuingOrganization { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string? Notes { get; set; }
}
