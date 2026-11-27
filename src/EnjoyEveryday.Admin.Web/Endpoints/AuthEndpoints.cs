using System.Security.Claims;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnjoyEveryday.Admin.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");

        group.MapGet("/activate", ([FromQuery] string? token, IConfiguration config) =>
        {
            var daycareUrl = config["DaycareUrl"] ?? "https://localhost:7003";
            return Results.Redirect($"{daycareUrl.TrimEnd('/')}/auth/activate?token={Uri.EscapeDataString(token ?? string.Empty)}");
        });

        group.MapGet("/login", (HttpContext context, [FromQuery] string? returnUrl, [FromQuery] string? inviteToken) =>
        {
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var clientId = config["Authentication:Google:ClientId"]
                        ?? config["Google:ClientId"]
                        ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

            if (string.IsNullOrWhiteSpace(clientId) || clientId == "mock-client-id")
            {
                if (IsDevLoginPermitted(context))
                {
                    return Results.Redirect($"/auth/dev-login?returnUrl={Uri.EscapeDataString(EnsureLocalUrl(returnUrl, "/"))}");
                }

                return Results.Redirect("/auth/access-denied?reason=google_not_configured");
            }

            var properties = new AuthenticationProperties { RedirectUri = $"/auth/callback?returnUrl={Uri.EscapeDataString(EnsureLocalUrl(returnUrl, "/"))}" };

            if (!string.IsNullOrEmpty(inviteToken))
            {
                properties.Items["InviteToken"] = inviteToken;
            }

            return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
        });

        group.MapGet("/dev-login", async (HttpContext context, IPortalAuthRepository authRepo, [FromQuery] string? returnUrl) =>
        {
            if (!IsDevLoginPermitted(context))
            {
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Security");
                logger.LogWarning("Unauthorized access attempt to /auth/dev-login from {Ip} on host {Host}", context.Connection.RemoteIpAddress, context.Request.Host);
                return Results.Redirect("/login");
            }

            var syncResult = await authRepo.DevLoginAsync("admin@littlestars.com");

            if (syncResult != null && syncResult.OutUserId.HasValue)
            {
                Guid userId = syncResult.OutUserId.Value;
                Guid effectiveTenantId = syncResult.OutEffectiveTenantId ?? syncResult.OutTenantId ?? Guid.Empty;
                Guid? activeOrgId = syncResult.OutActiveOrgId;
                string displayName = syncResult.OutDisplayName ?? syncResult.OutEmail ?? "Admin";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, displayName),
                    new Claim("DisplayName", displayName),
                    new Claim(ClaimTypes.Email, syncResult.OutEmail ?? "admin@littlestars.com"),
                    new Claim("TenantId", effectiveTenantId.ToString())
                };

                await BuildAuthClaimsAsync(authRepo, claims, userId, activeOrgId);

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
                return Results.Redirect(EnsureLocalUrl(returnUrl, "/select-organization"));
            }

            return Results.Redirect("/login");
        });

        group.MapGet("/callback", async (HttpContext context, IPortalAuthRepository authRepo, [FromQuery] string? returnUrl) =>
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

            var syncResult = await authRepo.SyncExternalLoginAsync(
                "Google",
                providerKey,
                email,
                givenName,
                surname,
                fullName,
                "Admin");

            if (syncResult is null || string.IsNullOrEmpty(syncResult.OutStatus) || syncResult.OutStatus != "success")
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var reason = syncResult?.OutStatus ?? "auth_failed";
                return Results.Redirect($"/access-denied?reason={reason}");
            }

            Guid userId = syncResult.OutUserId ?? Guid.Empty;
            Guid effectiveTenantId = syncResult.OutEffectiveTenantId ?? Guid.Empty;
            Guid? activeOrgId = syncResult.OutActiveOrgId;
            string displayName = syncResult.OutDisplayName ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, email ?? syncResult.OutEmail ?? string.Empty),
                new Claim("TenantId", effectiveTenantId.ToString())
            };

            await BuildAuthClaimsAsync(authRepo, claims, userId, activeOrgId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = true };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);

            if (activeOrgId == null)
            {
                return Results.Redirect("/select-organization");
            }

            return Results.Redirect(EnsureLocalUrl(returnUrl, "/"));
        });

        group.MapPost("/set-organization", async (HttpContext context, IPortalAuthRepository authRepo, [FromQuery] string? returnUrl) =>
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

            var validation = await authRepo.ValidateAndSetOrganizationAsync(userId, organizationId);

            if (validation == null || !validation.IsValid)
            {
                return Results.Redirect("/auth/access-denied?reason=invalid_org");
            }

            tenantId = validation.TenantId?.ToString() ?? tenantId ?? string.Empty;

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

            await BuildAuthClaimsAsync(authRepo, claims, userId, organizationId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties { IsPersistent = true };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);

            return Results.Redirect(EnsureLocalUrl(returnUrl, "/"));
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

    private static async Task BuildAuthClaimsAsync(IPortalAuthRepository authRepo, List<Claim> claims, Guid userId, Guid? activeOrgId)
    {
        if (activeOrgId.HasValue)
        {
            claims.Add(new Claim("ActiveOrganizationId", activeOrgId.Value.ToString()));
        }

        var authClaims = await authRepo.GetUserAuthClaimsAsync(userId, activeOrgId);

        foreach (var row in authClaims)
        {
            if (row.ClaimType == "role" && !string.IsNullOrWhiteSpace(row.ClaimValue))
            {
                claims.Add(new Claim(ClaimTypes.Role, row.ClaimValue));
            }
            else if (row.ClaimType == "permission" && !string.IsNullOrWhiteSpace(row.ClaimValue))
            {
                claims.Add(new Claim("Permission", row.ClaimValue));
            }
        }
    }

    private static bool IsDevLoginPermitted(HttpContext context)
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment()) return false;

        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var allowConfig = config.GetValue<bool?>("Authentication:AllowDevLogin")
                       ?? config.GetValue<bool?>("AllowDevLogin")
                       ?? true;
        if (!allowConfig) return false;

        var host = context.Request.Host.Host;
        bool isLocalHost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);
        if (!isLocalHost) return false;

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null && !System.Net.IPAddress.IsLoopback(remoteIp))
        {
            if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        return true;
    }

    private static string EnsureLocalUrl(string? returnUrl, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return fallback;
        if (returnUrl.StartsWith("//") || returnUrl.StartsWith("/\\")) return fallback;
        if (returnUrl.StartsWith("/") && Uri.TryCreate(returnUrl, UriKind.Relative, out _)) return returnUrl;
        return fallback;
    }
}
