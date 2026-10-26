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

        group.MapGet("/login", (HttpContext context, [FromQuery] string? returnUrl, [FromQuery] string? inviteToken) =>
        {
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var clientId = config["Authentication:Google:ClientId"]
                        ?? config["Google:ClientId"]
                        ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

            if (string.IsNullOrWhiteSpace(clientId) || clientId == "mock-client-id")
            {
                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                if (env.IsDevelopment())
                {
                    return Results.Redirect($"/auth/dev-login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
                }

                return Results.Redirect("/auth/access-denied?reason=google_not_configured");
            }

            var properties = new AuthenticationProperties { RedirectUri = $"/auth/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}" };
            
            if (!string.IsNullOrEmpty(inviteToken))
            {
                properties.Items["InviteToken"] = inviteToken;
            }

            return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
        });

        group.MapGet("/dev-login", async (HttpContext context, IDbConnectionFactory db, [FromQuery] string? returnUrl) =>
        {
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsDevelopment())
            {
                return Results.Redirect("/login");
            }

            using var conn = await db.CreateConnectionAsync();
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT id, email, display_name, first_name, last_name, tenant_id FROM users WHERE normalized_email = 'ADMIN@LITTLESTARS.COM' LIMIT 1")
                ?? await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT id, email, display_name, first_name, last_name, tenant_id FROM users LIMIT 1");
            if (user != null)
            {
                string displayName = (string?)user.display_name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = $"{user.first_name} {user.last_name}".Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = (string?)user.email ?? "Admin";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                    new Claim(ClaimTypes.Name, displayName),
                    new Claim("DisplayName", displayName),
                    new Claim(ClaimTypes.Email, user.email),
                    new Claim("TenantId", user.tenant_id.ToString())
                };

                var memberships = (await conn.QueryAsync<Guid>(
                    "SELECT organization_id FROM organization_memberships WHERE user_id = @UserId AND status = 'Active'",
                    new { UserId = (Guid)user.id })).ToList();
                Guid? activeOrgId = memberships.FirstOrDefault();

                await BuildAuthClaimsAsync(conn, claims, (Guid)user.id, activeOrgId);

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
                return Results.Redirect(returnUrl ?? "/select-organization");
            }

            return Results.Redirect("/login");
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

            var givenName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value 
                         ?? result.Principal.FindFirst(ClaimTypes.Name)?.Value 
                         ?? email.Split('@')[0];
            var surname = result.Principal.FindFirst(ClaimTypes.Surname)?.Value ?? "";
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? $"{givenName} {surname}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = email;

            using var conn = await db.CreateConnectionAsync();

            var syncResult = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM fn_sync_external_login(@Provider, @ProviderKey, @Email, @FirstName, @LastName, @FullName, 'Admin')",
                new {
                    Provider = "Google",
                    ProviderKey = providerKey,
                    Email = email,
                    FirstName = givenName,
                    LastName = surname,
                    FullName = fullName
                });

            if (syncResult == null || (string)syncResult.out_status != "success")
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var reason = (string?)syncResult?.out_status ?? "auth_failed";
                return Results.Redirect($"/access-denied?reason={reason}");
            }

            Guid userId = (Guid)syncResult.out_user_id;
            Guid effectiveTenantId = (Guid)syncResult.out_effective_tenant_id;
            Guid? activeOrgId = syncResult.out_active_org_id != null ? (Guid)syncResult.out_active_org_id : null;
            string displayName = (string)syncResult.out_display_name;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, email ?? (string)syncResult.out_email ?? string.Empty),
                new Claim("TenantId", effectiveTenantId.ToString())
            };

            await BuildAuthClaimsAsync(conn, claims, userId, activeOrgId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = true };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            
            if (activeOrgId == null)
            {
                return Results.Redirect("/select-organization");
            }

            return Results.Redirect(returnUrl ?? "/");
        });

        group.MapPost("/set-organization", async (HttpContext context, IDbConnectionFactory db, [FromQuery] string? returnUrl) =>
        {
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var tenantId = context.User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) 
                return Results.Redirect("/auth/login");

            Guid? organizationId = null;
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                if (Guid.TryParse(form["organizationId"], out var parsedOrgId))
                {
                    organizationId = parsedOrgId;
                }
            }

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

            var displayName = context.User.FindFirst(ClaimTypes.Name)?.Value 
                ?? context.User.FindFirst("DisplayName")?.Value 
                ?? email 
                ?? "Admin";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userIdStr),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, email ?? ""),
                new Claim("TenantId", tenantId ?? "")
            };

            await BuildAuthClaimsAsync(conn, claims, userId, organizationId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = true };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);

            return Results.Redirect(returnUrl ?? "/");
        }).DisableAntiforgery();

        group.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });
        
        group.MapGet("/access-denied", ([FromQuery] string? reason) => 
        {
            return Results.Redirect($"/access-denied?reason={Uri.EscapeDataString(reason ?? string.Empty)}");
        });
    }

    private static async Task BuildAuthClaimsAsync(System.Data.IDbConnection conn, List<Claim> claims, Guid userId, Guid? activeOrgId)
    {
        if (activeOrgId.HasValue)
        {
            claims.Add(new Claim("ActiveOrganizationId", activeOrgId.Value.ToString()));
        }

        var authClaims = await conn.QueryAsync<dynamic>(
            "SELECT * FROM fn_get_user_auth_claims(@UserId, @ActiveOrgId)",
            new { UserId = userId, ActiveOrgId = activeOrgId });

        foreach (var row in authClaims)
        {
            string type = (string)row.claim_type;
            string val = (string)row.claim_value;
            if (type == "role")
            {
                claims.Add(new Claim(ClaimTypes.Role, val));
            }
            else if (type == "permission")
            {
                claims.Add(new Claim("Permission", val));
            }
        }
    }
}
