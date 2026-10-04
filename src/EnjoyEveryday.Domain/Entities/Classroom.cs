namespace EnjoyEveryday.Domain.Entities;

public class Classroom
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ShortName { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    
    public string? ClassroomType { get; set; }
    public string Status { get; set; } = "Draft";
    
    public string? AgeGroup { get; set; }
    public int? MinAgeMonths { get; set; }
    public int? MaxAgeMonths { get; set; }
    
    public int? Capacity { get; set; }
    public int CurrentEnrollment { get; set; }
    
    public string Environment { get; set; } = "Indoor";
    public string? PhotoUrl { get; set; }
    
    public string? DraftData { get; set; }
    public int ProfileCompletionPercentage { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
