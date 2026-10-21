using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Hubs;

public class ClassroomChatHub : Hub
{
    private readonly ILogger<ClassroomChatHub> _logger;

    public ClassroomChatHub(ILogger<ClassroomChatHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinScope(string scopeType, string scopeId)
    {
        if (!string.IsNullOrWhiteSpace(scopeType) && !string.IsNullOrWhiteSpace(scopeId))
        {
            var groupName = $"{scopeType.ToLower().Trim()}_{scopeId.Trim()}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("SignalR Connection {ConnectionId} joined scope {GroupName}", Context.ConnectionId, groupName);
        }
    }

    public async Task LeaveScope(string scopeType, string scopeId)
    {
        if (!string.IsNullOrWhiteSpace(scopeType) && !string.IsNullOrWhiteSpace(scopeId))
        {
            var groupName = $"{scopeType.ToLower().Trim()}_{scopeId.Trim()}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("SignalR Connection {ConnectionId} left scope {GroupName}", Context.ConnectionId, groupName);
        }
    }
}
