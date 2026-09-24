namespace EnjoyEveryday.Shared.Audit;

/// <summary>
/// Service for recording audit log entries.
/// Every important operation should be auditable: WHO / WHAT / WHEN / WHERE / WHY / RESULT.
/// </summary>
public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

public record AuditEntry
{
    public Guid? TenantId { get; init; }
    public Guid? UserId { get; init; }
    public required string Action { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public object? Details { get; init; }
    public string? IpAddress { get; init; }
}
