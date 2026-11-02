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
/// PostgreSQL Dapper implementation of IAgentTokenRepository.
/// </summary>
public class AgentTokenRepository : IAgentTokenRepository
{
    static AgentTokenRepository()
    {
        SqlMapper.AddTypeHandler(new StringListTypeHandler());
    }

    private readonly IDbConnectionFactory _connectionFactory;

    public AgentTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<AgentAccessToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                id, tenant_id, user_id, token_name, token_prefix, token_hash, 
                role, scopes, is_revoked, expires_at, last_used_at, created_at, updated_at
            FROM agent_personal_access_tokens
            WHERE token_hash = @tokenHash AND is_revoked = FALSE;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AgentAccessToken>(
            new CommandDefinition(sql, new { tokenHash }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<AgentAccessToken>> GetUserTokensAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                id, tenant_id, user_id, token_name, token_prefix, 
                role, scopes, is_revoked, expires_at, last_used_at, created_at, updated_at
            FROM agent_personal_access_tokens
            WHERE tenant_id = @tenantId AND user_id = @userId
            ORDER BY created_at DESC;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<AgentAccessToken>(
            new CommandDefinition(sql, new { tenantId, userId }, cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateTokenAsync(AgentAccessToken token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO agent_personal_access_tokens (
                id, tenant_id, user_id, token_name, token_prefix, token_hash, 
                role, scopes, is_revoked, expires_at, created_at, updated_at
            )
            VALUES (
                @Id, @TenantId, @UserId, @TokenName, @TokenPrefix, @TokenHash, 
                @Role, @Scopes, FALSE, @ExpiresAt, NOW(), NOW()
            )
            RETURNING id;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, token, cancellationToken: cancellationToken));
    }

    public async Task<bool> RevokeTokenAsync(Guid tenantId, Guid tokenId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE agent_personal_access_tokens
            SET is_revoked = TRUE, updated_at = NOW()
            WHERE id = @tokenId AND tenant_id = @tenantId;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { tenantId, tokenId }, cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task UpdateLastUsedAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE agent_personal_access_tokens
            SET last_used_at = NOW()
            WHERE id = @tokenId;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { tokenId }, cancellationToken: cancellationToken));
    }
}

public class StringListTypeHandler : SqlMapper.TypeHandler<List<string>>
{
    public override void SetValue(System.Data.IDbDataParameter parameter, List<string>? value)
    {
        parameter.Value = value?.ToArray() ?? Array.Empty<string>();
    }

    public override List<string> Parse(object value)
    {
        if (value is string[] array) return new List<string>(array);
        if (value is IEnumerable<string> enumerable) return new List<string>(enumerable);
        if (value is string str) return new List<string> { str };
        return new List<string>();
    }
}
