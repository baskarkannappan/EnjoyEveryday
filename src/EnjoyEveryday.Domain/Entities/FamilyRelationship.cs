namespace EnjoyEveryday.Domain.Entities;

public class FamilyRelationship
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
    public Guid UserId { get; set; } // The Parent
    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
