using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public class BranchOperationalStatusDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool IsOperational { get; set; }
    public string OperationalStatus { get; set; } = "Open";
    public BranchClosure? ActiveClosure { get; set; }
}

public class ClassroomAvailabilityDto
{
    public Guid ClassroomId { get; set; }
    public Guid BranchId { get; set; }
    public bool IsClassroomIndependentlyActive { get; set; }
    public bool IsBranchOperational { get; set; }
    public bool IsEffectivelyAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public BranchClosure? ActiveBranchClosure { get; set; }
}

public class CloseBranchRequest
{
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string Status { get; set; } = "Closed";
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentAnnouncement { get; set; }
    public DateTimeOffset EffectiveStartAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EffectiveEndAt { get; set; }
    public Guid InitiatedBy { get; set; }
    public string InitiatedByRole { get; set; } = string.Empty;
}

public class ReopenBranchRequest
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ClosureId { get; set; }
    public Guid ReopenedBy { get; set; }
    public DateTimeOffset ReopenedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class BranchClosedException : InvalidOperationException
{
    public string ErrorCode => "BRANCH_CLOSED";
    public Guid BranchId { get; }
    public string OperationalStatus { get; }
    public DateTimeOffset? EffectiveStartAt { get; }
    public DateTimeOffset? EffectiveEndAt { get; }

    public BranchClosedException(
        Guid branchId,
        string operationalStatus,
        string message,
        DateTimeOffset? effectiveStartAt = null,
        DateTimeOffset? effectiveEndAt = null)
        : base(message)
    {
        BranchId = branchId;
        OperationalStatus = operationalStatus;
        EffectiveStartAt = effectiveStartAt;
        EffectiveEndAt = effectiveEndAt;
    }
}

public interface IBranchOperationalStatusService
{
    Task<BranchOperationalStatusDto> GetBranchStatusAsync(Guid tenantId, Guid branchId, DateTimeOffset? atTime = null, CancellationToken cancellationToken = default);
    Task<ClassroomAvailabilityDto> GetClassroomAvailabilityAsync(Guid tenantId, Guid classroomId, DateTimeOffset? atTime = null, CancellationToken cancellationToken = default);
    Task EnsureBranchOperationalAsync(Guid tenantId, Guid branchId, string operation, DateTimeOffset? effectiveAt = null, CancellationToken cancellationToken = default);
    Task EnsureClassroomOperationalAsync(Guid tenantId, Guid classroomId, string operation, DateTimeOffset? effectiveAt = null, CancellationToken cancellationToken = default);
    Task<BranchClosure> CloseBranchAsync(CloseBranchRequest request, CancellationToken cancellationToken = default);
    Task ReopenBranchAsync(ReopenBranchRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<BranchClosure>> GetClosureHistoryAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, CancellationToken cancellationToken = default);
}
