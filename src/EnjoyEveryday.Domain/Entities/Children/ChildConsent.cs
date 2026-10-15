namespace EnjoyEveryday.Domain.Entities;

public class ChildConsent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
}

