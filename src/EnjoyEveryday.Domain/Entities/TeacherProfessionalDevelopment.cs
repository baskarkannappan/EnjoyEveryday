namespace EnjoyEveryday.Domain.Entities;

public class TeacherProfessionalDevelopment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal HoursCompleted { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? Notes { get; set; }
}
