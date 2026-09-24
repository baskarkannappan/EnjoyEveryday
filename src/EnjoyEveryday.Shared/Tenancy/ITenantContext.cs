namespace EnjoyEveryday.Shared.Tenancy;

/// <summary>
/// Represents the current tenant context for the request.
/// Propagated through the entire request pipeline.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantName { get; }
    bool IsResolved { get; }
}

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public bool IsResolved => TenantId != Guid.Empty;
}
