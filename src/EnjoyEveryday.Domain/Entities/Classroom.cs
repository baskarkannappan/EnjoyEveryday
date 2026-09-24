namespace EnjoyEveryday.Domain.Entities;

public class Classroom
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AgeGroup { get; set; }
    public int? Capacity { get; set; }
    public string Environment { get; set; } = "Indoor";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
