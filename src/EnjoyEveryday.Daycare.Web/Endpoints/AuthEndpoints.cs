using System.Security.Claims;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnjoyEveryday.Daycare.Web.Endpoints;

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

        group.MapGet("/dev-login", async (HttpContext context, IDbConnectionFactory db, [FromQuery] string? returnUrl, [FromQuery] string? email) =>
        {
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsDevelopment())
            {
                return Results.Redirect("/login");
            }

            using var conn = await db.CreateConnectionAsync();
            var syncResult = await conn.QuerySingleFunctionAsync<AuthSyncResult>(
                "fn_dev_login",
                new { Email = email?.Trim() });

            if (syncResult == null || syncResult.out_status != "success" || !syncResult.out_user_id.HasValue)
            {
                return Results.Redirect("/login");
            }

            Guid userId = syncResult.out_user_id.Value;
            Guid effectiveTenantId = syncResult.out_effective_tenant_id ?? syncResult.out_tenant_id ?? Guid.Empty;
            Guid? activeOrgId = syncResult.out_active_org_id;
            string displayName = syncResult.out_display_name ?? syncResult.out_email ?? "Teacher";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, syncResult.out_email ?? string.Empty),
                new Claim("TenantId", effectiveTenantId.ToString())
            };

            await BuildAuthClaimsAsync(conn, claims, userId, activeOrgId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
            return Results.Redirect(returnUrl ?? "/");
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

            var givenName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value 
                         ?? result.Principal.FindFirst(ClaimTypes.Name)?.Value 
                         ?? email.Split('@')[0];
            var surname = result.Principal.FindFirst(ClaimTypes.Surname)?.Value ?? "";
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? $"{givenName} {surname}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = email;

            using var conn = await db.CreateConnectionAsync();

            var syncResult = await conn.QuerySingleFunctionAsync<AuthSyncResult>(
                "fn_sync_external_login",
                new
                {
                    Provider = "Google",
                    ProviderKey = providerKey,
                    Email = email,
                    FirstName = givenName,
                    LastName = surname,
                    DisplayName = fullName ?? givenName,
                    Portal = "Daycare"
                });

            if (syncResult == null || syncResult.out_status != "success" || !syncResult.out_user_id.HasValue)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var reason = syncResult?.out_status ?? "auth_failed";
                return Results.Redirect($"/access-denied?reason={reason}");
            }

            Guid userId = syncResult.out_user_id.Value;
            Guid effectiveTenantId = syncResult.out_effective_tenant_id ?? syncResult.out_tenant_id ?? Guid.Empty;
            Guid? activeOrgId = syncResult.out_active_org_id;
            string displayName = syncResult.out_display_name ?? fullName ?? givenName;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, email ?? syncResult.out_email ?? string.Empty),
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
            var validation = await conn.QuerySingleFunctionAsync<OrgValidationResult>(
                "fn_validate_and_set_organization",
                new { UserId = userId, OrganizationId = organizationId });

            if (validation == null || !validation.is_valid)
            {
                return Results.Redirect("/auth/access-denied?reason=invalid_org");
            }

            tenantId = validation.tenant_id?.ToString() ?? tenantId ?? string.Empty;

            var displayName = context.User.FindFirst(ClaimTypes.Name)?.Value 
                ?? context.User.FindFirst("DisplayName")?.Value 
                ?? email 
                ?? "Teacher";

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

        var authClaims = await conn.QueryFunctionAsync<AuthClaimRow>(
            "fn_get_user_auth_claims",
            new { UserId = userId, ActiveOrgId = activeOrgId });

        foreach (var row in authClaims)
        {
            if (row.claim_type == "role" && !string.IsNullOrWhiteSpace(row.claim_value))
            {
                claims.Add(new Claim(ClaimTypes.Role, row.claim_value));
            }
            else if (row.claim_type == "permission" && !string.IsNullOrWhiteSpace(row.claim_value))
            {
                claims.Add(new Claim("Permission", row.claim_value));
            }
        }
    }

    private sealed class AuthSyncResult
    {
        public string? out_status { get; set; }
        public Guid? out_user_id { get; set; }
        public Guid? out_tenant_id { get; set; }
        public Guid? out_effective_tenant_id { get; set; }
        public Guid? out_active_org_id { get; set; }
        public string? out_email { get; set; }
        public string? out_display_name { get; set; }
    }

    private sealed class OrgValidationResult
    {
        public bool is_valid { get; set; }
        public Guid? tenant_id { get; set; }
    }

    private sealed class AuthClaimRow
    {
        public string? claim_type { get; set; }
        public string? claim_value { get; set; }
    }
}
