namespace EnjoyEveryday.Application.Services;

public interface IBranchStatusRealtimeNotifier
{
    Task NotifyBranchStatusChangedAsync(Guid organizationId, Guid branchId, string status, CancellationToken cancellationToken = default);
}

