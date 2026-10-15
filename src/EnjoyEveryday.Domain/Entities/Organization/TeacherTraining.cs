namespace EnjoyEveryday.Domain.Entities;

public class TeacherTraining
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public string TrainingName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public DateTime? CompletionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Status { get; set; }
}
