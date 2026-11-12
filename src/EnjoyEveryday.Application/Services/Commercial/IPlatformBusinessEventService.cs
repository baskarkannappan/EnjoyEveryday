using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public class PublishBusinessEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? InitiatedByUserId { get; set; }
    public string? TransactionReference { get; set; }
    public string TransactionCategory { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "Completed";
    public object? Metadata { get; set; }
    public string? ActionUrl { get; set; }
    public bool SendPlatformAdminEmail { get; set; } = true;
    public string? EmailSubject { get; set; }
    public string? EmailHtmlBody { get; set; }
}

public interface IPlatformBusinessEventService
{
    Task<PlatformBusinessEvent> PublishEventAsync(PublishBusinessEventRequest request, CancellationToken ct = default);
    Task<List<PlatformBusinessEvent>> GetRecentEventsAsync(int limit = 50, int offset = 0, string? category = null, string? eventType = null, Guid? organizationId = null, CancellationToken ct = default);
    Task<List<PlatformAdminNotification>> GetAdminNotificationsAsync(Guid userId, bool unreadOnly = false, int limit = 30, CancellationToken ct = default);
    Task<int> GetUnreadNotificationCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkNotificationReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllNotificationsReadAsync(Guid userId, CancellationToken ct = default);
    Task<string?> ResolveOrganizationAdminEmailAsync(Guid organizationId, CancellationToken ct = default);
}

