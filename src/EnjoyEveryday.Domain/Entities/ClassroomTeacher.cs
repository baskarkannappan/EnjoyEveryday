namespace EnjoyEveryday.Domain.Entities;

public class ClassroomTeacher
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassroomId { get; set; }
    public Guid UserId { get; set; } // The Teacher
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
