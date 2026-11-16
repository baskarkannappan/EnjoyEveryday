using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;

namespace EnjoyEveryday.Application.Services;

public class BranchOperationalStatusService : IBranchOperationalStatusService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IBranchClosureRepository _closureRepository;

    public BranchOperationalStatusService(
        IBranchRepository branchRepository,
        IClassroomRepository classroomRepository,
        IBranchClosureRepository closureRepository)
    {
        _branchRepository = branchRepository;
        _classroomRepository = classroomRepository;
        _closureRepository = closureRepository;
    }

    public async Task<BranchOperationalStatusDto> GetBranchStatusAsync(Guid tenantId, Guid branchId, DateTimeOffset? atTime = null, CancellationToken cancellationToken = default)
    {
        var checkTime = atTime ?? DateTimeOffset.UtcNow;
        var branch = await _branchRepository.GetByIdAsync(tenantId, branchId, cancellationToken);
        if (branch == null)
        {
            return new BranchOperationalStatusDto
            {
                BranchId = branchId,
                IsOperational = false,
                OperationalStatus = "Closed"
            };
        }

        var activeClosure = await _closureRepository.GetActiveClosureAsync(tenantId, branchId, checkTime, cancellationToken);
        var isClosedByWindow = activeClosure != null;
        var isClosedByStatus = !string.Equals(branch.OperationalStatus, "Open", StringComparison.OrdinalIgnoreCase) && atTime == null;
        var isOperational = branch.IsActive && !isClosedByWindow && !isClosedByStatus;

        return new BranchOperationalStatusDto
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            IsOperational = isOperational,
            OperationalStatus = activeClosure?.Status ?? branch.OperationalStatus,
            ActiveClosure = activeClosure
        };
    }

    public async Task<ClassroomAvailabilityDto> GetClassroomAvailabilityAsync(Guid tenantId, Guid classroomId, DateTimeOffset? atTime = null, CancellationToken cancellationToken = default)
    {
        var classroom = await _classroomRepository.GetByIdAsync(tenantId, classroomId, cancellationToken);
        if (classroom == null)
        {
            return new ClassroomAvailabilityDto
            {
                ClassroomId = classroomId,
                IsEffectivelyAvailable = false,
                UnavailableReason = "CLASSROOM_NOT_FOUND"
            };
        }

        var branchStatus = await GetBranchStatusAsync(tenantId, classroom.BranchId, atTime, cancellationToken);
        var isClassroomActive = classroom.IsActive && !string.Equals(classroom.Status, "Inactive", StringComparison.OrdinalIgnoreCase);
        var isAvailable = branchStatus.IsOperational && isClassroomActive;

        string? reason = null;
        if (!branchStatus.IsOperational)
        {
            reason = "BRANCH_CLOSED";
        }
        else if (!isClassroomActive)
        {
            reason = "CLASSROOM_INACTIVE";
        }

        return new ClassroomAvailabilityDto
        {
            ClassroomId = classroom.Id,
            BranchId = classroom.BranchId,
            IsClassroomIndependentlyActive = isClassroomActive,
            IsBranchOperational = branchStatus.IsOperational,
            IsEffectivelyAvailable = isAvailable,
            UnavailableReason = reason,
            ActiveBranchClosure = branchStatus.ActiveClosure
        };
    }

    public async Task EnsureBranchOperationalAsync(Guid tenantId, Guid branchId, string operation, DateTimeOffset? effectiveAt = null, CancellationToken cancellationToken = default)
    {
        var status = await GetBranchStatusAsync(tenantId, branchId, effectiveAt, cancellationToken);
        if (!status.IsOperational)
        {
            throw new BranchClosedException(
                branchId,
                status.OperationalStatus,
                $"This operation ({operation}) cannot be completed because the branch is currently {status.OperationalStatus.ToLowerInvariant()}.",
                status.ActiveClosure?.EffectiveStartAt,
                status.ActiveClosure?.EffectiveEndAt);
        }
    }

    public async Task EnsureClassroomOperationalAsync(Guid tenantId, Guid classroomId, string operation, DateTimeOffset? effectiveAt = null, CancellationToken cancellationToken = default)
    {
        var availability = await GetClassroomAvailabilityAsync(tenantId, classroomId, effectiveAt, cancellationToken);
        if (!availability.IsBranchOperational)
        {
            throw new BranchClosedException(
                availability.BranchId,
                availability.ActiveBranchClosure?.Status ?? "Closed",
                $"This operation ({operation}) cannot be completed because the parent branch is currently closed.",
                availability.ActiveBranchClosure?.EffectiveStartAt,
                availability.ActiveBranchClosure?.EffectiveEndAt);
        }

        if (!availability.IsClassroomIndependentlyActive)
        {
            throw new InvalidOperationException($"This operation ({operation}) cannot be completed because the classroom is inactive.");
        }
    }

    public async Task<BranchClosure> CloseBranchAsync(CloseBranchRequest request, CancellationToken cancellationToken = default)
    {
        var classrooms = await _classroomRepository.GetByBranchIdAsync(request.TenantId, request.BranchId, cancellationToken);
        var affectedCount = classrooms.Count();

        var closure = new BranchClosure
        {
            TenantId = request.TenantId,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Closed" : request.Status,
            Reason = request.Reason,
            Description = request.Description,
            ParentAnnouncement = request.ParentAnnouncement,
            EffectiveStartAt = request.EffectiveStartAt,
            EffectiveEndAt = request.EffectiveEndAt,
            CreatedBy = request.InitiatedBy,
            CreatedByRole = request.InitiatedByRole,
            AffectedClassroomCount = affectedCount,
            CancelledSessionCount = 0
        };

        return await _closureRepository.CreateClosureAsync(closure, cancellationToken);
    }

    public async Task ReopenBranchAsync(ReopenBranchRequest request, CancellationToken cancellationToken = default)
    {
        await _closureRepository.MarkReopenedAsync(request.TenantId, request.ClosureId, request.ReopenedBy, request.ReopenedAt, cancellationToken);
    }

    public async Task<IEnumerable<BranchClosure>> GetClosureHistoryAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await _closureRepository.GetHistoryAsync(tenantId, organizationId, branchId, cancellationToken);
    }
}
