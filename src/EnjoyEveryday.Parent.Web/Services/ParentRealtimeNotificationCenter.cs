using System;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Parent.Web.Services;

public class ParentRealtimeNotificationCenter
{
    private readonly ILogger<ParentRealtimeNotificationCenter> _logger;

    public ParentRealtimeNotificationCenter(ILogger<ParentRealtimeNotificationCenter> logger)
    {
        _logger = logger;
    }

    public event Func<ParentMessageNotification, Task>? OnEducatorMessageReceived;

    public async Task BroadcastEducatorMessageAsync(ParentMessageNotification notification)
    {
        _logger.LogInformation("Broadcasting educator message from {SenderName} for child {ChildName}", notification.SenderName, notification.ChildName);
        if (OnEducatorMessageReceived != null)
        {
            foreach (Func<ParentMessageNotification, Task> handler in OnEducatorMessageReceived.GetInvocationList())
            {
                try
                {
                    await handler(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error executing OnEducatorMessageReceived notification handler");
                }
            }
        }
    }
}
