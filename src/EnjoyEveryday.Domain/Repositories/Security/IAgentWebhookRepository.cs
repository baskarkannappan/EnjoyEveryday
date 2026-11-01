using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities.Security;

namespace EnjoyEveryday.Domain.Repositories;

/// <summary>
/// Repository contract for managing AI Agent Webhook Subscriptions.
/// </summary>
public interface IAgentWebhookRepository
{
    Task<IEnumerable<AgentWebhookSubscription>> GetActiveSubscriptionsForEventAsync(Guid tenantId, string eventType, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentWebhookSubscription>> GetTenantSubscriptionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateSubscriptionAsync(AgentWebhookSubscription subscription, CancellationToken cancellationToken = default);
    Task<bool> DeleteSubscriptionAsync(Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task RecordDeliveryResultAsync(Guid subscriptionId, bool success, CancellationToken cancellationToken = default);
}
