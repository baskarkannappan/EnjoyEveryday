namespace EnjoyEveryday.Domain.Entities;

public class Person
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public string? Email { get; set; }
    public string? MobilePhone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool HasPortalAccess { get; set; }
    public bool ReceivesDailyUpdates { get; set; }
    public bool ReceivesPhotos { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
