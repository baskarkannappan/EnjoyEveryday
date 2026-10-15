namespace EnjoyEveryday.Domain.Entities;

public class ChildMilestone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid TeacherId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset MilestoneDate { get; set; } = DateTimeOffset.UtcNow;
    
    public Guid? DevelopmentAreaId { get; set; }
    public string? EvidenceSummary { get; set; }
    public Guid? JourneyEntryId { get; set; }
    
    public bool ParentVisible { get; set; }
    public string Status { get; set; } = "Active";
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
}
