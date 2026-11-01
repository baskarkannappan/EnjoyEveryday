using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Api.Authentication;
using EnjoyEveryday.Domain.Entities.Security;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers.Security;

public record CreateAgentTokenRequest(
    string TokenName,
    int? ExpiresInDays = 90,
    List<string>? CustomScopes = null);

public record AgentTokenCreatedResponse(
    Guid Id,
    string TokenName,
    string TokenPrefix,
    string RawToken,
    string Role,
    List<string> Scopes,
    DateTime? ExpiresAt,
    DateTime CreatedAt);

public record AgentTokenSummaryDto(
    Guid Id,
    string TokenName,
    string TokenPrefix,
    string Role,
    List<string> Scopes,
    bool IsRevoked,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime CreatedAt);

[ApiController]
[Route("api/v1/agent-tokens")]
[Authorize]
public class AgentTokensController : ControllerBase
{
    private readonly IAgentTokenRepository _tokenRepo;

    public AgentTokensController(IAgentTokenRepository tokenRepo)
    {
        _tokenRepo = tokenRepo ?? throw new ArgumentNullException(nameof(tokenRepo));
    }

    private (Guid TenantId, Guid UserId, string Role) GetCallerIdentity()
    {
        var tenantStr = User.FindFirstValue("tenant_id") ?? "10000000-0000-0000-0000-000000000001";
        var userStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "50000000-0000-0000-0000-000000000002";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? RoleTemplates.OrganizationAdmin;

        return (Guid.Parse(tenantStr), Guid.Parse(userStr), role);
    }

    [HttpPost]
    public async Task<ActionResult<AgentTokenCreatedResponse>> CreateToken(
        [FromBody] CreateAgentTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "TokenName is required."
            });
        }

        var (tenantId, userId, role) = GetCallerIdentity();

        // Generate 32 bytes cryptographically secure random token
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var base64Random = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var rawToken = $"{AgentAuthenticationDefaults.TokenPrefix}{base64Random}";
        var tokenPrefix = $"{rawToken[..Math.Min(14, rawToken.Length)]}...";
        var tokenHash = AgentTokenAuthenticationHandler.HashToken(rawToken);

        var scopes = request.CustomScopes != null && request.CustomScopes.Count > 0
            ? request.CustomScopes
            : AgentTokenScopes.GetDefaultScopesForRole(role);

        DateTime? expiresAt = request.ExpiresInDays.HasValue && request.ExpiresInDays.Value > 0
            ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
            : null;

        var token = new AgentAccessToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            TokenName = request.TokenName.Trim(),
            TokenPrefix = tokenPrefix,
            TokenHash = tokenHash,
            Role = role,
            Scopes = scopes,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdId = await _tokenRepo.CreateTokenAsync(token, cancellationToken);

        return Ok(new AgentTokenCreatedResponse(
            createdId,
            token.TokenName,
            token.TokenPrefix,
            rawToken,
            token.Role,
            token.Scopes,
            token.ExpiresAt,
            token.CreatedAt));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgentTokenSummaryDto>>> GetUserTokens(CancellationToken cancellationToken)
    {
        var (tenantId, userId, _) = GetCallerIdentity();
        var tokens = await _tokenRepo.GetUserTokensAsync(tenantId, userId, cancellationToken);

        var result = new List<AgentTokenSummaryDto>();
        foreach (var t in tokens)
        {
            result.Add(new AgentTokenSummaryDto(
                t.Id,
                t.TokenName,
                t.TokenPrefix,
                t.Role,
                t.Scopes,
                t.IsRevoked,
                t.ExpiresAt,
                t.LastUsedAt,
                t.CreatedAt));
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeToken(Guid id, CancellationToken cancellationToken)
    {
        var (tenantId, userId, _) = GetCallerIdentity();
        var revoked = await _tokenRepo.RevokeTokenAsync(tenantId, id, userId, cancellationToken);
        if (!revoked)
        {
            return NotFound();
        }
        return NoContent();
    }
}
