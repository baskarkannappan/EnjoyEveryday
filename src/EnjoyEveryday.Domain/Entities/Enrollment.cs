namespace EnjoyEveryday.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
}

