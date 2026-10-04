namespace EnjoyEveryday.Domain.Entities;

public class Teacher
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? UserId { get; set; }
    
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PreferredName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    
    public string PreferredLanguage { get; set; } = string.Empty;
    public string OtherLanguages { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public string? Bio { get; set; }
    
    public string Status { get; set; } = "Draft"; // Draft, Pending Verification, Active, etc.
    public string? DraftData { get; set; }
    public int ProfileCompletionPercentage { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
