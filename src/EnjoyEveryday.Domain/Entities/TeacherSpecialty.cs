namespace EnjoyEveryday.Domain.Entities;
public class TeacherSpecialty
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SpecialtyId { get; set; }
    public string Level { get; set; } = string.Empty;
    public int? YearsExperience { get; set; }
    public string? Notes { get; set; }
}
