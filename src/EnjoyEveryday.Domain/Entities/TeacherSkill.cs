namespace EnjoyEveryday.Domain.Entities;
public class TeacherSkill
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SkillId { get; set; }
    public string SkillLevel { get; set; } = string.Empty;
    public int? YearsExperience { get; set; }
    public bool IsPrimarySkill { get; set; }
    public bool Verified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }
}
