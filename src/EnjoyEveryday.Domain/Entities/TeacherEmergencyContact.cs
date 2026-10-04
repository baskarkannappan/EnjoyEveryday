namespace EnjoyEveryday.Domain.Entities;
public class TeacherEmergencyContact
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PrimaryPhone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int Priority { get; set; }
    public string? Notes { get; set; }
}
