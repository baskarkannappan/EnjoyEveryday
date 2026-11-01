using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities.Security;

namespace EnjoyEveryday.Domain.Repositories;

/// <summary>
/// Repository contract for managing Agent Personal Access Tokens.
/// </summary>
public interface IAgentTokenRepository
{
    Task<AgentAccessToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentAccessToken>> GetUserTokensAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<Guid> CreateTokenAsync(AgentAccessToken token, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(Guid tenantId, Guid tokenId, Guid userId, CancellationToken cancellationToken = default);
    Task UpdateLastUsedAsync(Guid tokenId, CancellationToken cancellationToken = default);
}
