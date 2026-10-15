namespace EnjoyEveryday.Domain.Entities;
public class TeacherCertification
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string CertificationType { get; set; } = string.Empty;
    public string CertificationName { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string? Notes { get; set; }
}
