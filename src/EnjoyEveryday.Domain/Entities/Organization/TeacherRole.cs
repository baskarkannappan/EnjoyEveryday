namespace EnjoyEveryday.Domain.Entities;

public class TeacherRole
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid RoleId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
