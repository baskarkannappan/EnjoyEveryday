using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class PlatformBusinessEventRepository : IPlatformBusinessEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PlatformBusinessEventRepository> _logger;

    public PlatformBusinessEventRepository(IDbConnectionFactory connectionFactory, ILogger<PlatformBusinessEventRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<PlatformBusinessEvent> RecordBusinessEventAsync(PlatformBusinessEvent businessEvent, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO public.platform_business_events (
                id, event_type, tenant_id, organization_id, branch_id, initiated_by_user_id,
                transaction_reference, transaction_category, title, description,
                amount, currency, status, event_payload, created_at
            ) VALUES (
                @Id, @EventType, @TenantId, @OrganizationId, @BranchId, @InitiatedByUserId,
                @TransactionReference, @TransactionCategory, @Title, @Description,
                @Amount, @Currency, @Status, @EventPayload::jsonb, @CreatedAt
            );";

        var payload = string.IsNullOrWhiteSpace(businessEvent.EventPayload) ? "{}" : businessEvent.EventPayload;

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            businessEvent.Id,
            businessEvent.EventType,
            businessEvent.TenantId,
            businessEvent.OrganizationId,
            businessEvent.BranchId,
            businessEvent.InitiatedByUserId,
            businessEvent.TransactionReference,
            businessEvent.TransactionCategory,
            businessEvent.Title,
            businessEvent.Description,
            businessEvent.Amount,
            businessEvent.Currency,
            businessEvent.Status,
            EventPayload = payload,
            CreatedAt = businessEvent.CreatedAt.UtcDateTime
        }, cancellationToken: ct));

        return businessEvent;
    }

    public async Task<List<PlatformBusinessEvent>> GetBusinessEventsAsync(
        int limit = 50,
        int offset = 0,
        string? category = null,
        string? eventType = null,
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<PlatformBusinessEvent>(
            "fn_get_platform_business_events",
            new
            {
                p_limit = limit,
                p_offset = offset,
                p_category = category,
                p_event_type = eventType,
                p_organization_id = organizationId
            },
            cancellationToken: ct);

        return rows.ToList();
    }

    public async Task<PlatformAdminNotification> CreateNotificationAsync(PlatformAdminNotification notification, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO public.platform_admin_notifications (
                id, business_event_id, user_id, title, message, action_url, is_read, read_at, created_at
            ) VALUES (
                @Id, @BusinessEventId, @UserId, @Title, @Message, @ActionUrl, @IsRead, @ReadAt, @CreatedAt
            );";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            notification.Id,
            notification.BusinessEventId,
            notification.UserId,
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.IsRead,
            ReadAt = notification.ReadAt?.UtcDateTime,
            CreatedAt = notification.CreatedAt.UtcDateTime
        }, cancellationToken: ct));

        return notification;
    }

    public async Task<List<PlatformAdminNotification>> GetNotificationsAsync(
        Guid userId,
        bool unreadOnly = false,
        int limit = 30,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<PlatformAdminNotification>(
            "fn_get_platform_admin_notifications",
            new
            {
                p_user_id = userId,
                p_unread_only = unreadOnly,
                p_limit = limit
            },
            cancellationToken: ct);

        return rows.ToList();
    }

    public async Task<int> GetUnreadNotificationCountAsync(Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM public.platform_admin_notifications WHERE (user_id = @UserId OR user_id IS NULL) AND is_read = FALSE;";
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    public async Task MarkNotificationReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_mark_platform_notification_read",
            new { p_notification_id = notificationId, p_user_id = userId },
            cancellationToken: ct);
    }

    public async Task MarkAllNotificationsReadAsync(Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_mark_all_platform_notifications_read",
            new { p_user_id = userId },
            cancellationToken: ct);
    }

    public async Task<(Guid UserId, string Email, string DisplayName)?> GetOrgAdminEmailForOrgAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = (await conn.QueryFunctionAsync<OrgAdminRow>(
            "fn_get_org_admin_email_for_org",
            new { p_organization_id = organizationId },
            cancellationToken: ct)).FirstOrDefault();

        if (row != null && !string.IsNullOrWhiteSpace(row.email))
        {
            return (row.user_id, row.email, row.display_name ?? row.email);
        }

        return null;
    }

    private class OrgAdminRow
    {
        public Guid user_id { get; set; }
        public string email { get; set; } = string.Empty;
        public string? display_name { get; set; }
    }
}

