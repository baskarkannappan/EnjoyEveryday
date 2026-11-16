using System;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Services;

public record BranchStatusChangedNotification(Guid OrganizationId, Guid BranchId, string Status, DateTimeOffset ChangedAtUtc);

public class DaycareRealtimeNotificationCenter
{
    private readonly ILogger<DaycareRealtimeNotificationCenter> _logger;

    public DaycareRealtimeNotificationCenter(ILogger<DaycareRealtimeNotificationCenter> logger)
    {
        _logger = logger;
    }

    public event Func<ParentMessageNotification, Task>? OnParentMessageReceived;
    public event Func<Guid, Task>? OnMessagesRead;
    public event Func<BranchStatusChangedNotification, Task>? OnBranchStatusChanged;

    public async Task BroadcastParentMessageAsync(ParentMessageNotification notification)
    {
        _logger.LogInformation("Broadcasting parent message from {SenderName} for child {ChildName}", notification.SenderName, notification.ChildName);
        if (OnParentMessageReceived != null)
        {
            foreach (Func<ParentMessageNotification, Task> handler in OnParentMessageReceived.GetInvocationList())
            {
                try
                {
                    await handler(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error executing OnParentMessageReceived notification handler");
                }
            }
        }
    }

    public async Task NotifyMessagesReadAsync(Guid childId)
    {
        if (OnMessagesRead != null)
        {
            foreach (Func<Guid, Task> handler in OnMessagesRead.GetInvocationList())
            {
                try
                {
                    await handler(childId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error executing OnMessagesRead notification handler");
                }
            }
        }
    }

    public async Task BroadcastBranchStatusChangedAsync(Guid organizationId, Guid branchId, string status)
    {
        _logger.LogInformation("Broadcasting branch status change: Org={OrganizationId}, Branch={BranchId}, Status={Status}", organizationId, branchId, status);
        if (OnBranchStatusChanged != null)
        {
            var payload = new BranchStatusChangedNotification(organizationId, branchId, status, DateTimeOffset.UtcNow);
            foreach (Func<BranchStatusChangedNotification, Task> handler in OnBranchStatusChanged.GetInvocationList())
            {
                try
                {
                    await handler(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error executing OnBranchStatusChanged notification handler");
                }
            }
        }
    }
}
