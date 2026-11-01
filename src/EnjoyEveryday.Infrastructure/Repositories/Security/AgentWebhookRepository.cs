using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities.Security;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories.Security;

/// <summary>
/// PostgreSQL Dapper implementation of IAgentWebhookRepository.
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
        const string sql = """
            SELECT 
                id, tenant_id, user_id, name, webhook_url, secret_key, 
                subscribed_events, is_active, last_triggered_at, failure_count, 
                created_at, updated_at
            FROM agent_webhook_subscriptions
            WHERE tenant_id = @tenantId 
              AND is_active = TRUE
              AND (@eventType = ANY(subscribed_events) OR '*' = ANY(subscribed_events));
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<AgentWebhookSubscription>(
            new CommandDefinition(sql, new { tenantId, eventType }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<AgentWebhookSubscription>> GetTenantSubscriptionsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                id, tenant_id, user_id, name, webhook_url, secret_key, 
                subscribed_events, is_active, last_triggered_at, failure_count, 
                created_at, updated_at
            FROM agent_webhook_subscriptions
            WHERE tenant_id = @tenantId
            ORDER BY created_at DESC;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<AgentWebhookSubscription>(
            new CommandDefinition(sql, new { tenantId }, cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateSubscriptionAsync(
        AgentWebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO agent_webhook_subscriptions (
                id, tenant_id, user_id, name, webhook_url, secret_key, 
                subscribed_events, is_active, failure_count, created_at, updated_at
            )
            VALUES (
                @Id, @TenantId, @UserId, @Name, @WebhookUrl, @SecretKey, 
                @SubscribedEvents, TRUE, 0, NOW(), NOW()
            )
            RETURNING id;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, subscription, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteSubscriptionAsync(
        Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM agent_webhook_subscriptions
            WHERE id = @subscriptionId AND tenant_id = @tenantId;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { tenantId, subscriptionId }, cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task RecordDeliveryResultAsync(
        Guid subscriptionId, bool success, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE agent_webhook_subscriptions
            SET last_triggered_at = NOW(),
                failure_count = CASE WHEN @success THEN 0 ELSE failure_count + 1 END,
                is_active = CASE WHEN @success THEN TRUE WHEN failure_count >= 10 THEN FALSE ELSE TRUE END,
                updated_at = NOW()
            WHERE id = @subscriptionId;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { subscriptionId, success }, cancellationToken: cancellationToken));
    }
}
