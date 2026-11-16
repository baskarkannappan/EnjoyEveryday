namespace EnjoyEveryday.Domain.Entities;

public class BranchClosure
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string Status { get; set; } = "Closed";
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentAnnouncement { get; set; }
    public DateTimeOffset EffectiveStartAt { get; set; }
    public DateTimeOffset? EffectiveEndAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ReopenedBy { get; set; }
    public DateTimeOffset? ReopenedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int AffectedClassroomCount { get; set; }
    public int CancelledSessionCount { get; set; }
}

