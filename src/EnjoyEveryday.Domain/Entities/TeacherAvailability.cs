namespace EnjoyEveryday.Domain.Entities;

public class TeacherAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeacherId { get; set; }
    public bool CanWorkMornings { get; set; }
    public bool CanWorkAfternoons { get; set; }
    public bool CanWorkEvenings { get; set; }
    public bool CanWorkWeekends { get; set; }
    public decimal MaxHoursPerWeek { get; set; }
    public string? Notes { get; set; }
}
