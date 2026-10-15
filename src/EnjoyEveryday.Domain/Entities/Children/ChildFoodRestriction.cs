namespace EnjoyEveryday.Domain.Entities;

public class ChildFoodRestriction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
}

