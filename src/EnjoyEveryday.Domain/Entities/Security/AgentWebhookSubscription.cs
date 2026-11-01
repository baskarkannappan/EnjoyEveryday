using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities.Security;

/// <summary>
/// Represents a webhook subscription where external AI agents receive real-time daycare domain events.
/// </summary>
public class AgentWebhookSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public List<string> SubscribedEvents { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggeredAt { get; set; }
    public int FailureCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
