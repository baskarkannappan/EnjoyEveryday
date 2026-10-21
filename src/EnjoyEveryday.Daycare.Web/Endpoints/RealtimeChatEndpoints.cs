using System;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Daycare.Web.Hubs;
using EnjoyEveryday.Daycare.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Endpoints;

public static class RealtimeChatEndpoints
{
    public static IEndpointRouteBuilder MapRealtimeChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/realtime/parent-message", async (
            ParentMessageNotification notification,
            IHubContext<ClassroomChatHub> hubContext,
            DaycareRealtimeNotificationCenter notificationCenter,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("RealtimeChatEndpoints");
            logger.LogInformation("Real-time parent message received at Daycare portal for child {ChildName} (Org: {OrgId})", notification.ChildName, notification.OrganizationId);

            // 1. Notify in-process Blazor Interactive Server circuits (NavMenu, ClassroomMessages)
            await notificationCenter.BroadcastParentMessageAsync(notification);

            // 2. Broadcast via SignalR hub to any connected external clients
            await hubContext.Clients.Group($"child_{notification.ChildId}").SendAsync("ReceiveParentMessage", notification);
            await hubContext.Clients.Group($"tenant_{notification.TenantId}").SendAsync("ReceiveParentMessage", notification);
            await hubContext.Clients.Group($"org_{notification.OrganizationId}").SendAsync("ReceiveParentMessage", notification);

            if (notification.BranchId.HasValue)
            {
                await hubContext.Clients.Group($"branch_{notification.BranchId.Value}").SendAsync("ReceiveParentMessage", notification);
            }

            if (notification.ClassroomId.HasValue)
            {
                await hubContext.Clients.Group($"classroom_{notification.ClassroomId.Value}").SendAsync("ReceiveParentMessage", notification);
            }

            return Results.Ok(new { success = true, timestamp = DateTimeOffset.UtcNow });
        }).DisableAntiforgery();

        return app;
    }
}
