using EnjoyEveryday.Application.Services;

namespace EnjoyEveryday.Daycare.Web.Services;

public sealed class BranchStatusRealtimeNotifier : IBranchStatusRealtimeNotifier
{
    private readonly DaycareRealtimeNotificationCenter _notificationCenter;

    public BranchStatusRealtimeNotifier(DaycareRealtimeNotificationCenter notificationCenter)
    {
        _notificationCenter = notificationCenter;
    }

    public Task NotifyBranchStatusChangedAsync(Guid organizationId, Guid branchId, string status, CancellationToken cancellationToken = default)
    {
        return _notificationCenter.BroadcastBranchStatusChangedAsync(organizationId, branchId, status);
    }
}

