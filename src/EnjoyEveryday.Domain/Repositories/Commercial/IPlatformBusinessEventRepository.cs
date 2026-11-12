using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IPlatformBusinessEventRepository
{
    Task<PlatformBusinessEvent> RecordBusinessEventAsync(PlatformBusinessEvent businessEvent, CancellationToken ct = default);
    Task<List<PlatformBusinessEvent>> GetBusinessEventsAsync(int limit = 50, int offset = 0, string? category = null, string? eventType = null, Guid? organizationId = null, CancellationToken ct = default);
    Task<PlatformAdminNotification> CreateNotificationAsync(PlatformAdminNotification notification, CancellationToken ct = default);
    Task<List<PlatformAdminNotification>> GetNotificationsAsync(Guid userId, bool unreadOnly = false, int limit = 30, CancellationToken ct = default);
    Task<int> GetUnreadNotificationCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkNotificationReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllNotificationsReadAsync(Guid userId, CancellationToken ct = default);
    Task<(Guid UserId, string Email, string DisplayName)?> GetOrgAdminEmailForOrgAsync(Guid organizationId, CancellationToken ct = default);
}

