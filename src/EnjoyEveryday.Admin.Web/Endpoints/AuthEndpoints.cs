using System.Security.Claims;
using Dapper;
using EnjoyEveryday.Shared.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace EnjoyEveryday.Admin.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");

        group.MapGet("/login", async (HttpContext context, IDbConnectionFactory db, [FromQuery] string? returnUrl, [FromQuery] string? inviteToken) =>
        {
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
                using var conn = await db.CreateConnectionAsync();
                var user = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT id, email, tenant_id FROM users LIMIT 1");
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                        new Claim(ClaimTypes.Email, user.email),
                        new Claim("TenantId", user.tenant_id.ToString())
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
                    return Results.Redirect("/select-organization");
                }
            }

            var properties = new AuthenticationProperties { RedirectUri = $"/auth/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}" };
            
            if (!string.IsNullOrEmpty(inviteToken))
            {
                properties.Items["InviteToken"] = inviteToken;
            }

            return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
        });

        group.MapGet("/callback", async (HttpContext context, IDbConnectionFactory db, [FromQuery] string? returnUrl) =>
        {
            var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
                return Results.Redirect("/auth/access-denied?reason=auth_failed");

            var providerKey = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(providerKey) || string.IsNullOrEmpty(email))
                return Results.Redirect("/auth/access-denied?reason=invalid_claims");

            var inviteToken = result.Properties?.Items.ContainsKey("InviteToken") == true ? result.Properties.Items["InviteToken"] : null;

            using var conn = await db.CreateConnectionAsync();

            // 1. Check if external login is already linked
            var userId = await conn.ExecuteScalarAsync<Guid?>(
                "SELECT user_id FROM user_external_logins WHERE provider = 'Google' AND provider_key = @ProviderKey",
                new { ProviderKey = providerKey });

            if (userId == null)
            {
                // Not linked. Check if they have an invite token.
                if (string.IsNullOrEmpty(inviteToken))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect("/auth/access-denied?reason=not_linked");
                }

                // Verify the invite token against the user table (we'll use security_stamp as the invite token for now)
                var invitedUser = await conn.QuerySingleOrDefaultAsync<Guid?>(
                    "SELECT id FROM users WHERE email = @Email AND security_stamp = @Token AND is_active = true",
                    new { Email = email, Token = inviteToken });

                if (invitedUser == null)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect("/auth/access-denied?reason=invalid_invite");
                }

                userId = invitedUser.Value;

                // Atomically link the account and clear the invite token
                await conn.ExecuteAsync(@"
                    INSERT INTO user_external_logins (provider, provider_key, user_id) VALUES ('Google', @ProviderKey, @UserId);
                    UPDATE users SET security_stamp = encode(gen_random_bytes(32), 'hex'), email_confirmed = true WHERE id = @UserId;
                ", new { ProviderKey = providerKey, UserId = userId });
            }

            // 2. We have a linked user ID. Ensure the account is active.
            var user = await conn.QuerySingleOrDefaultAsync(
                "SELECT id, tenant_id, is_active FROM users WHERE id = @UserId", 
                new { UserId = userId });

            if (user == null || !user.is_active)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=account_disabled");
            }

            // 3. Rebuild the claims explicitly from our trusted DB
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("TenantId", user.tenant_id.ToString())
            };

            var memberships = (await conn.QueryAsync<Guid>(
                "SELECT organization_id FROM organization_memberships WHERE user_id = @UserId AND status = 'Active'",
                new { UserId = user.id })).ToList();

            if (memberships.Count == 0)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=no_organizations");
            }

            Guid? activeOrgId = memberships.Count == 1 ? memberships[0] : null;

            await BuildAuthClaimsAsync(conn, claims, user.id, activeOrgId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = true };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            
            if (activeOrgId == null)
            {
                return Results.Redirect("/select-organization");
            }

            return Results.Redirect(returnUrl ?? "/");
        });

        group.MapPost("/set-organization", async (HttpContext context, IDbConnectionFactory db, [FromForm] Guid? organizationId, [FromQuery] string? returnUrl) =>
        {
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var tenantId = context.User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) 
                return Results.Redirect("/auth/login");

            using var conn = await db.CreateConnectionAsync();
            
            if (organizationId.HasValue)
            {
                var isActive = await conn.ExecuteScalarAsync<bool>(
                    "SELECT true FROM organization_memberships WHERE user_id = @UserId AND organization_id = @OrgId AND status = 'Active'",
                    new { UserId = userId, OrgId = organizationId.Value });

                if (!isActive) return Results.Redirect("/auth/access-denied?reason=invalid_org");
            }
            else
            {
                var isParent = await conn.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM family_relationships WHERE user_id = @UserId)", 
                    new { UserId = userId });
                    
                if (!isParent) return Results.Redirect("/auth/access-denied?reason=invalid_org");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userIdStr),
                new Claim(ClaimTypes.Email, email ?? ""),
                new Claim("TenantId", tenantId ?? "")
            };

            await BuildAuthClaimsAsync(conn, claims, userId, organizationId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = true };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);

            return Results.Redirect(returnUrl ?? "/");
        });

        group.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });
        
        group.MapGet("/access-denied", ([FromQuery] string? reason) => 
        {
            var message = reason switch 
            {
                "not_linked" => "Your account is not linked. Please use the invitation link sent by your administrator.",
                "invalid_invite" => "The invitation link is invalid or expired.",
                "account_disabled" => "Your account is disabled. Contact your administrator.",
                "no_organizations" => "You are not assigned to any active organizations.",
                "invalid_org" => "Invalid organization selection.",
                _ => "Access Denied."
            };
            return Results.Text(message, "text/plain");
        });
    }

    private static async Task BuildAuthClaimsAsync(System.Data.IDbConnection conn, List<Claim> claims, Guid userId, Guid? activeOrgId)
    {
        if (activeOrgId.HasValue)
        {
            claims.Add(new Claim("ActiveOrganizationId", activeOrgId.Value.ToString()));
        }

        var roles = await conn.QueryAsync<string>(
            @"SELECT r.name 
              FROM user_roles ur JOIN roles r ON ur.role_id = r.id 
              WHERE ur.user_id = @UserId
              UNION
              SELECT r.name 
              FROM user_organization_roles uor JOIN roles r ON uor.role_id = r.id 
              WHERE uor.user_id = @UserId AND uor.organization_id = @ActiveOrgId",
            new { UserId = userId, ActiveOrgId = activeOrgId });

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var permissions = await conn.QueryAsync<string>(
            @"SELECT p.name 
              FROM user_roles ur 
              JOIN role_permissions rp ON ur.role_id = rp.role_id 
              JOIN permissions p ON rp.permission_id = p.id 
              WHERE ur.user_id = @UserId
              UNION
              SELECT p.name 
              FROM user_organization_roles uor 
              JOIN role_permissions rp ON uor.role_id = rp.role_id 
              JOIN permissions p ON rp.permission_id = p.id 
              WHERE uor.user_id = @UserId AND uor.organization_id = @ActiveOrgId",
            new { UserId = userId, ActiveOrgId = activeOrgId });

        foreach (var perm in permissions)
            claims.Add(new Claim("Permission", perm));
    }
}
