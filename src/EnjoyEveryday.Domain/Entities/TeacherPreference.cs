namespace EnjoyEveryday.Domain.Entities;

public class TeacherPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public string PreferenceType { get; set; } = string.Empty;
    public string PreferenceValue { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
