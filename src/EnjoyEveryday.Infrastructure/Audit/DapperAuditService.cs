using System.Text.Json;
using Dapper;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Audit;

/// <summary>
/// PostgreSQL implementation of IAuditService using Dapper.
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
        const string sql = """
            INSERT INTO audit_log (tenant_id, user_id, action, entity_type, entity_id, details, ip_address)
            VALUES (@TenantId, @UserId, @Action, @EntityType, @EntityId, @Details::jsonb, @IpAddress)
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new
        {
            entry.TenantId,
            entry.UserId,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            Details = entry.Details != null ? JsonSerializer.Serialize(entry.Details) : null,
            entry.IpAddress
        });
    }
}
