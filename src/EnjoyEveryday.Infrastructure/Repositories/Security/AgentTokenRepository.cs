using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities.Security;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories.Security;

/// <summary>
/// PostgreSQL Dapper implementation of IAgentTokenRepository using stored procedures/functions.
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
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<AgentAccessToken>(
            "fn_get_agent_token_by_hash",
            new { p_token_hash = tokenHash },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<AgentAccessToken>> GetUserTokensAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<AgentAccessToken>(
            "fn_get_agent_tokens_by_user",
            new { p_tenant_id = tenantId, p_user_id = userId },
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> CreateTokenAsync(AgentAccessToken token, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_create_agent_token",
            new
            {
                p_id = token.Id,
                p_tenant_id = token.TenantId,
                p_user_id = token.UserId,
                p_token_name = token.TokenName,
                p_token_prefix = token.TokenPrefix,
                p_token_hash = token.TokenHash,
                p_role = token.Role,
                p_scopes = token.Scopes?.ToArray() ?? Array.Empty<string>(),
                p_expires_at = token.ExpiresAt
            },
            cancellationToken: cancellationToken);
        return token.Id;
    }

    public async Task<bool> RevokeTokenAsync(Guid tenantId, Guid tokenId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_revoke_agent_token",
            new { p_tenant_id = tenantId, p_token_id = tokenId },
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task UpdateLastUsedAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "sp_update_agent_token_last_used",
            new { p_token_id = tokenId },
            cancellationToken: cancellationToken);
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
