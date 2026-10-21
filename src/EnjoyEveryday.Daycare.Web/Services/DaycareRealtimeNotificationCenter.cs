using System;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Services;

public class DaycareRealtimeNotificationCenter
{
    private readonly ILogger<DaycareRealtimeNotificationCenter> _logger;

    public DaycareRealtimeNotificationCenter(ILogger<DaycareRealtimeNotificationCenter> logger)
    {
        _logger = logger;
    }

    public event Func<ParentMessageNotification, Task>? OnParentMessageReceived;
    public event Func<Guid, Task>? OnMessagesRead;

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
}
