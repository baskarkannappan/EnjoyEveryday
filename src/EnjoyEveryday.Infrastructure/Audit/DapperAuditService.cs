using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Audit;

/// <summary>
/// PostgreSQL implementation of IAuditService using stored procedure sp_log_audit_entry.
/// </summary>
public class DapperAuditService : IAuditService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAuditService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_log_audit_entry",
            new
            {
                entry.TenantId,
                entry.UserId,
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                Details = entry.Details != null ? JsonSerializer.Serialize(entry.Details) : null,
                entry.IpAddress
            },
            parameterNames: new[]
            {
                "TenantId", "UserId", "Action", "EntityType", "EntityId", "Details::jsonb", "IpAddress"
            },
            cancellationToken: cancellationToken);
    }
}
