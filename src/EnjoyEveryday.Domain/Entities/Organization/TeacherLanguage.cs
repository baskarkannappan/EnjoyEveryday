namespace EnjoyEveryday.Domain.Entities;
public class TeacherLanguage
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Proficiency { get; set; } = string.Empty;
    public bool IsPrimaryLanguage { get; set; }
    public bool CanCommunicateWithParents { get; set; }
    public bool CanTeachInLanguage { get; set; }
}
