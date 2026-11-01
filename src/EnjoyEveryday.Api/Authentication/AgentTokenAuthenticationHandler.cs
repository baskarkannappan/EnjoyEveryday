using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnjoyEveryday.Api.Authentication;

public static class AgentAuthenticationDefaults
{
    public const string AuthenticationScheme = "AgentBearer";
    public const string TokenPrefix = "eed_pat_";
}

/// <summary>
/// Authenticates external AI agents using hashed Personal Access Tokens (PAT).
/// </summary>
public class AgentTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAgentTokenRepository _tokenRepo;

    public AgentTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAgentTokenRepository tokenRepo)
        : base(options, logger, encoder)
    {
        _tokenRepo = tokenRepo ?? throw new ArgumentNullException(nameof(tokenRepo));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrWhiteSpace(authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var headerValue = authHeader.ToString().Trim();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var rawToken = headerValue.Substring("Bearer ".Length).Trim();
        if (!rawToken.StartsWith(AgentAuthenticationDefaults.TokenPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // Not an agent PAT token, pass to next authentication handler (e.g. standard JWT if any)
            return AuthenticateResult.NoResult();
        }

        var tokenHash = HashToken(rawToken);
        var token = await _tokenRepo.GetByHashAsync(tokenHash, Context.RequestAborted);

        if (token == null || !token.IsActive)
        {
            return AuthenticateResult.Fail("Invalid, revoked, or expired agent personal access token.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, token.UserId.ToString()),
            new(ClaimTypes.Role, token.Role),
            new("tenant_id", token.TenantId.ToString()),
            new("token_id", token.Id.ToString()),
            new("token_name", token.TokenName),
            new("is_agent", "true")
        };

        foreach (var scope in token.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Update last used timestamp
        _ = _tokenRepo.UpdateLastUsedAsync(token.Id, CancellationToken.None);

        return AuthenticateResult.Success(ticket);
    }

    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
