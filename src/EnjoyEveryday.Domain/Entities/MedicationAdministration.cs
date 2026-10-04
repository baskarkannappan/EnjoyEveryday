namespace EnjoyEveryday.Domain.Entities;

public class MedicationAdministration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
}

