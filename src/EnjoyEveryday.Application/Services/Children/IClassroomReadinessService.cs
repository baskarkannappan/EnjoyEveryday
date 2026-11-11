using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public record ReadinessActionItem(string Title, string Message, string ActionUrl, string ActionText);

public record ClassroomReadinessCheckResult
{
    public bool IsReady { get; init; }
    public string Status { get; init; } = "Setup Required"; // Ready, Setup Required, Full, Inactive

    // Branch level checks
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public bool BranchExists { get; init; }
    public bool BranchActive { get; init; }
    public bool HasClassrooms { get; init; }
    public bool HasLeadTeacher { get; init; }

    // Classroom level checks (null if evaluating only branch)
    public Guid? ClassroomId { get; init; }
    public string? ClassroomName { get; init; }
    public bool ClassroomExists { get; init; }
    public bool ClassroomActive { get; init; }
    public bool HasAssignedTeacher { get; init; }
    public bool HasAvailableCapacity { get; init; }
    public int Capacity { get; init; }
    public int CurrentEnrolled { get; init; }
    public int AvailableSpots => Math.Max(0, Capacity - CurrentEnrolled);

    // Messages and direct links for UI remediation
    public List<string> MissingRequirements { get; init; } = new();
    public List<ReadinessActionItem> ActionItems { get; init; } = new();
}

public class ClassroomReadinessException : InvalidOperationException
{
    public ClassroomReadinessCheckResult Result { get; }

    public ClassroomReadinessException(ClassroomReadinessCheckResult result)
        : base(string.Join(" ", result.MissingRequirements))
    {
        Result = result;
    }
}

public interface IClassroomReadinessService
{
    Task<ClassroomReadinessCheckResult> ValidateBranchReadinessAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);

    Task<ClassroomReadinessCheckResult> ValidateClassroomReadinessAsync(Guid tenantId, Guid branchId, Guid classroomId, CancellationToken ct = default);

    Task<Dictionary<Guid, ClassroomReadinessCheckResult>> GetBranchClassroomsReadinessAsync(Guid tenantId, Guid branchId, CancellationToken ct = default);

    Task EnsureClassroomReadyForEnrollmentAsync(Guid tenantId, Guid branchId, Guid classroomId, CancellationToken ct = default);
}

