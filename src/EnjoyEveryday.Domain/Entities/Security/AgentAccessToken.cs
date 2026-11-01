using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities.Security;

/// <summary>
/// Represents a Personal Access Token (PAT) used by autonomous AI agents to interact with the platform.
/// </summary>
public class AgentAccessToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string TokenName { get; set; } = string.Empty;
    public string TokenPrefix { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public bool IsRevoked { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive => !IsRevoked && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);
}
