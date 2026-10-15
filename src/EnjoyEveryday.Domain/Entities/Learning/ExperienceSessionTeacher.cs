namespace EnjoyEveryday.Domain.Entities;

public class ExperienceSessionTeacher
{
    public Guid Id { get; set; }
    public Guid ExperienceSessionId { get; set; }
    public Guid TeacherId { get; set; }
    public string Role { get; set; } = "LeadTeacher";
}
