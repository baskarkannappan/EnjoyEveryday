using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities.Security;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories.Security;

/// <summary>
/// PostgreSQL Dapper implementation of IAgentWebhookRepository using stored procedures/functions.
/// </summary>
public class AgentWebhookRepository : IAgentWebhookRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AgentWebhookRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<AgentWebhookSubscription>> GetActiveSubscriptionsForEventAsync(
        Guid tenantId, string eventType, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<AgentWebhookSubscription>(
            "fn_get_active_agent_webhooks_for_event",
            new { p_tenant_id = tenantId, p_event_type = eventType },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<AgentWebhookSubscription>> GetTenantSubscriptionsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<AgentWebhookSubscription>(
            "fn_get_agent_webhooks",
            new { p_tenant_id = tenantId },
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> CreateSubscriptionAsync(
        AgentWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_create_agent_webhook",
            new
            {
                p_id = subscription.Id,
                p_tenant_id = subscription.TenantId,
                p_user_id = subscription.UserId,
                p_name = subscription.Name,
                p_webhook_url = subscription.WebhookUrl,
                p_secret_key = subscription.SecretKey,
                p_subscribed_events = subscription.SubscribedEvents?.ToArray() ?? Array.Empty<string>()
            },
            cancellationToken: cancellationToken);
        return subscription.Id;
    }

    public async Task<bool> DeleteSubscriptionAsync(
        Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_delete_agent_webhook",
            new { p_tenant_id = tenantId, p_subscription_id = subscriptionId },
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task RecordDeliveryResultAsync(
        Guid subscriptionId, bool success, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE agent_webhook_subscriptions
            SET last_triggered_at = NOW(),
                failure_count = CASE WHEN @success THEN 0 ELSE failure_count + 1 END,
                is_active = CASE WHEN @success THEN TRUE ELSE (failure_count + 1 < 10) END,
                updated_at = NOW()
            WHERE id = @subscriptionId;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { subscriptionId, success }, cancellationToken: cancellationToken));
    }
}
