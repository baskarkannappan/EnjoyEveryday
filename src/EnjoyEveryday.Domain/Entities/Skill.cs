namespace EnjoyEveryday.Domain.Entities;
public class Skill
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
}
