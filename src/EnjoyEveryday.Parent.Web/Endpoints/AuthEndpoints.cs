using System.Security.Claims;
using Dapper;
using EnjoyEveryday.Infrastructure.Data;
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

namespace EnjoyEveryday.Parent.Web.Endpoints;

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

            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            if (!Guid.TryParse(config["DevUserId"], out var userId))
                userId = Guid.Parse("50000000-0000-0000-0000-000000000010");

            if (!Guid.TryParse(config["DevTenantId"], out var tenantId))
                tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

            string email = "parent@littlestars.com";
            var childId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

            try
            {
                using var conn = await db.CreateConnectionAsync();
                var devSecurityStamp = Guid.NewGuid().ToString("N");
                await conn.ExecuteProcedureAsync(
                    "sp_seed_dev_parent_environment",
                    new
                    {
                        p_user_id = userId,
                        p_tenant_id = tenantId,
                        p_email = email,
                        p_child_id = childId,
                        p_security_stamp = devSecurityStamp
                    });
            }
            catch
            {
                // Fallback gracefully
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("TenantId", tenantId.ToString()),
                new Claim(ClaimTypes.Role, "Parent"),
                new Claim("Permission", "family.view")
            };

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

            var inviteToken = result.Properties?.Items.ContainsKey("InviteToken") == true ? result.Properties.Items["InviteToken"] : null;

            var givenName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value 
                         ?? result.Principal.FindFirst(ClaimTypes.Name)?.Value 
                         ?? email.Split('@')[0];
            var surname = result.Principal.FindFirst(ClaimTypes.Surname)?.Value ?? "";
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? $"{givenName} {surname}".Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = email;

            using var conn = await db.CreateConnectionAsync();

            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            if (!Guid.TryParse(config["DevTenantId"], out var defaultTenantId))
                defaultTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

            if (!Guid.TryParse(config["DevOrganizationId"], out var defaultOrgId))
                defaultOrgId = Guid.Parse("20000000-0000-0000-0000-000000000001");

            var dynParams = new DynamicParameters();
            dynParams.Add("p_provider", "Google");
            dynParams.Add("p_provider_key", providerKey);
            dynParams.Add("p_email", email);
            dynParams.Add("p_first_name", givenName);
            dynParams.Add("p_last_name", surname);
            dynParams.Add("p_display_name", fullName);
            dynParams.Add("p_default_tenant_id", defaultTenantId);
            dynParams.Add("p_default_org_id", defaultOrgId);
            dynParams.Add("io_user_id", dbType: System.Data.DbType.Guid, direction: System.Data.ParameterDirection.InputOutput);
            dynParams.Add("io_tenant_id", dbType: System.Data.DbType.Guid, direction: System.Data.ParameterDirection.InputOutput);
            dynParams.Add("io_is_active", dbType: System.Data.DbType.Boolean, direction: System.Data.ParameterDirection.InputOutput);

            await conn.ExecuteAsync("CALL sp_sync_parent_external_login(@p_provider, @p_provider_key, @p_email, @p_first_name, @p_last_name, @p_display_name, @p_default_tenant_id, @p_default_org_id, @io_user_id, @io_tenant_id, @io_is_active)", dynParams);

            var userId = dynParams.Get<Guid?>("io_user_id");
            var tenantId = dynParams.Get<Guid?>("io_tenant_id") ?? defaultTenantId;
            var isActive = dynParams.Get<bool?>("io_is_active") ?? false;

            if (userId == null || userId == Guid.Empty || !isActive)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=account_disabled");
            }

            // Sync parent-child relationships and person profile
            await conn.ExecuteProcedureAsync(
                "sp_sync_parent_relationships",
                new
                {
                    p_user_id = userId.Value,
                    p_email = email,
                    p_first_name = givenName,
                    p_last_name = surname,
                    p_tenant_id = tenantId
                });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Email, email ?? string.Empty),
                new Claim("TenantId", tenantId.ToString()),
                new Claim(ClaimTypes.Role, "Parent"),
                new Claim("Permission", "family.view")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

            return Results.Redirect(returnUrl ?? "/");
        });

        group.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });

        group.MapGet("/access-denied", ([FromQuery] string? reason) => 
        {
            var message = reason switch 
            {
                "not_linked" => "Your account is not linked. Please use the invitation link sent by your daycare administrator.",
                "invalid_invite" => "The invitation link is invalid or expired.",
                "account_disabled" => "Your account is disabled. Contact your daycare administrator.",
                "not_a_parent" => "No active guardian relationship was found for this account. Contact your daycare administrator.",
                "google_not_configured" => "Google Authentication is not yet configured. Please set Authentication:Google:ClientId in appsettings.json or use dev login.",
                _ => "Access Denied."
            };
            return Results.Text(message, "text/plain");
        });
    }
}
