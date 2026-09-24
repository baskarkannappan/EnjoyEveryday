namespace EnjoyEveryday.Domain.Entities;

public class Branch
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
