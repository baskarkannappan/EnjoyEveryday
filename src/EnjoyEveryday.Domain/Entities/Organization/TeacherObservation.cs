namespace EnjoyEveryday.Domain.Entities;

public class TeacherObservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public Guid ObserverUserId { get; set; }
    public DateTime ObservationDate { get; set; }
    public string Rating { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public string? ActionItems { get; set; }
}
