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
                if (IsDevLoginPermitted(context))
                {
                    return Results.Redirect($"/auth/dev-login?returnUrl={Uri.EscapeDataString(EnsureLocalUrl(returnUrl, "/"))}");
                }

                return Results.Redirect("/auth/access-denied?reason=google_not_configured");
            }

            var properties = new AuthenticationProperties { RedirectUri = $"/auth/callback?returnUrl={Uri.EscapeDataString(EnsureLocalUrl(returnUrl, "/"))}" };
            properties.SetParameter("prompt", "select_account");

            if (!string.IsNullOrEmpty(inviteToken))
            {
                properties.Items["InviteToken"] = inviteToken;
            }

            return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
        });

        group.MapGet("/dev-login", async (HttpContext context, IPortalAuthRepository authRepo, IBranchRepository branchRepo, [FromQuery] string? returnUrl, [FromQuery] string? email) =>
        {
            if (!IsDevLoginPermitted(context))
            {
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Security");
                logger.LogWarning("Unauthorized access attempt to /auth/dev-login from {Ip} on host {Host}", context.Connection.RemoteIpAddress, context.Request.Host);
                return Results.Redirect("/login");
            }

            var syncResult = await authRepo.DevLoginAsync(email);

            if (syncResult == null || syncResult.OutStatus != "success" || !syncResult.OutUserId.HasValue)
            {
                return Results.Redirect("/login");
            }

            Guid userId = syncResult.OutUserId.Value;
            Guid effectiveTenantId = syncResult.OutEffectiveTenantId ?? syncResult.OutTenantId ?? Guid.Empty;
            Guid? activeOrgId = syncResult.OutActiveOrgId;
            string displayName = syncResult.OutDisplayName ?? syncResult.OutEmail ?? "Teacher";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, syncResult.OutEmail ?? string.Empty),
                new Claim("TenantId", effectiveTenantId.ToString())
            };

            await BuildAuthClaimsAsync(authRepo, claims, userId, activeOrgId);

            return await FinalizeSignInAndRedirectAsync(context, branchRepo, claims, userId, activeOrgId, returnUrl, new AuthenticationProperties { IsPersistent = true });
        });

        group.MapGet("/callback", async (HttpContext context, IPortalAuthRepository authRepo, IBranchRepository branchRepo, [FromQuery] string? returnUrl) =>
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
                fullName ?? givenName,
                "Daycare");

            if (syncResult == null || syncResult.OutStatus != "success" || !syncResult.OutUserId.HasValue)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var reason = syncResult?.OutStatus ?? "auth_failed";
                return Results.Redirect($"/access-denied?reason={reason}");
            }

            Guid userId = syncResult.OutUserId.Value;
            Guid effectiveTenantId = syncResult.OutEffectiveTenantId ?? syncResult.OutTenantId ?? Guid.Empty;
            Guid? activeOrgId = syncResult.OutActiveOrgId;
            string displayName = !string.IsNullOrWhiteSpace(fullName) && fullName != email
                ? fullName
                : (syncResult.OutDisplayName ?? fullName ?? givenName);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, email ?? syncResult.OutEmail ?? string.Empty),
                new Claim("TenantId", effectiveTenantId.ToString())
            };

            if (result.Properties?.Items.TryGetValue("InviteToken", out var inviteToken) == true && !string.IsNullOrWhiteSpace(inviteToken))
            {
                await authRepo.AcceptOrganizationInvitationAsync(inviteToken, userId);
            }

            await BuildAuthClaimsAsync(authRepo, claims, userId, activeOrgId);

            var properties = new AuthenticationProperties { IsPersistent = true };
            return await FinalizeSignInAndRedirectAsync(context, branchRepo, claims, userId, activeOrgId, returnUrl, properties);
        });

        group.MapGet("/accept-invite", async (HttpContext context, IPortalAuthRepository authRepo, IBranchRepository branchRepo, [FromQuery] string token, [FromQuery] string? returnUrl) =>
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Redirect("/auth/activate?error=missing_token");
            }

            var val = await authRepo.ValidateOrganizationInvitationAsync(token);

            if (val == null || !val.Valid || !val.OrganizationId.HasValue || !val.TenantId.HasValue)
            {
                return Results.Redirect($"/auth/activate?token={Uri.EscapeDataString(token)}&error=invalid_token");
            }

            Guid orgId = val.OrganizationId.Value;
            Guid tenantId = val.TenantId.Value;
            string adminEmail = val.AdminEmail;
            string orgName = val.OrganizationName;

            var syncResult = await authRepo.SyncExternalLoginAsync(
                "Invitation",
                token.Trim(),
                adminEmail,
                "Admin",
                orgName,
                $"{adminEmail} ({orgName})",
                "Daycare");

            Guid userId;
            if (syncResult != null && syncResult.OutUserId.HasValue)
            {
                userId = syncResult.OutUserId.Value;
            }
            else
            {
                var foundUserId = await authRepo.GetUserIdByEmailAsync(adminEmail);
                if (foundUserId.HasValue)
                {
                    userId = foundUserId.Value;
                }
                else
                {
                    return Results.Redirect($"/auth/activate?token={Uri.EscapeDataString(token)}&error=user_creation_failed");
                }
            }

            await authRepo.AcceptOrganizationInvitationAsync(token, userId);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, $"{adminEmail} ({orgName})"),
                new Claim("DisplayName", $"{adminEmail} ({orgName})"),
                new Claim(ClaimTypes.Email, adminEmail),
                new Claim("TenantId", tenantId.ToString()),
                new Claim(ClaimTypes.Role, "OrganizationAdmin")
            };

            await BuildAuthClaimsAsync(authRepo, claims, userId, orgId);

            var properties = new AuthenticationProperties { IsPersistent = true };
            return await FinalizeSignInAndRedirectAsync(context, branchRepo, claims, userId, orgId, returnUrl ?? "/management/branch-dashboard", properties);
        });

        group.MapPost("/set-organization", async (HttpContext context, IPortalAuthRepository authRepo, IBranchRepository branchRepo, [FromQuery] string? returnUrl) =>
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
                ?? "Teacher";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userIdStr),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("DisplayName", displayName),
                new Claim(ClaimTypes.Email, email ?? ""),
                new Claim("TenantId", tenantId ?? "")
            };

            await BuildAuthClaimsAsync(authRepo, claims, userId, organizationId);

            var properties = new AuthenticationProperties { IsPersistent = true };
            return await FinalizeSignInAndRedirectAsync(context, branchRepo, claims, userId, organizationId, returnUrl, properties);
        }).DisableAntiforgery();

        group.MapPost("/set-branch", async (HttpContext context, IPortalAuthRepository authRepo, IBranchRepository branchRepo, [FromQuery] string? returnUrl) =>
        {
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Results.Redirect("/auth/login");

            var activeOrgIdStr = context.User.FindFirst("ActiveOrganizationId")?.Value;
            if (string.IsNullOrEmpty(activeOrgIdStr) || !Guid.TryParse(activeOrgIdStr, out var orgId))
                return Results.Redirect("/select-organization");

            Guid? branchId = null;
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                if (Guid.TryParse(form["branchId"], out var parsedBranchId))
                {
                    branchId = parsedBranchId;
                }
            }

            if (!branchId.HasValue)
                return Results.Redirect("/select-branch");

            var assignedBranches = (await branchRepo.GetAssignedForUserAsync(userId, orgId)).ToList();
            var matchedBranch = assignedBranches.FirstOrDefault(b => b.Id == branchId.Value);
            (Guid Id, string Name)? targetBranch = matchedBranch != null ? (matchedBranch.Id, matchedBranch.Name) : null;

            if (targetBranch == null)
            {
                var isOrgAdmin = context.User.IsInRole("OrganizationAdmin") || context.User.IsInRole("OrganizationOwner") || context.User.IsInRole("PlatformAdmin");
                if (isOrgAdmin)
                {
                    targetBranch = await authRepo.GetActiveBranchInOrganizationAsync(branchId.Value, orgId);
                }
            }

            if (targetBranch == null)
            {
                return Results.Redirect("/auth/access-denied?reason=unauthorized_branch");
            }

            var claims = context.User.Claims
                .Where(c => c.Type != "ActiveBranchId" && c.Type != "ActiveBranchName")
                .ToList();

            claims.Add(new Claim("ActiveBranchId", targetBranch.Value.Id.ToString()));
            claims.Add(new Claim("ActiveBranchName", targetBranch.Value.Name));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

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

    private static async Task<IResult> FinalizeSignInAndRedirectAsync(
        HttpContext context,
        IBranchRepository branchRepo,
        List<Claim> claims,
        Guid userId,
        Guid? activeOrgId,
        string? returnUrl,
        AuthenticationProperties properties)
    {
        if (activeOrgId == null)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            return Results.Redirect("/select-organization");
        }

        var isOrgAdmin = claims.Any(c => c.Type == ClaimTypes.Role &&
            (c.Value == "OrganizationAdmin" || c.Value == "OrganizationOwner" || c.Value == "PlatformAdmin"));

        if (isOrgAdmin)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            return Results.Redirect(EnsureLocalUrl(returnUrl, "/"));
        }

        var assignedBranches = (await branchRepo.GetAssignedForUserAsync(userId, activeOrgId.Value)).ToList();

        if (assignedBranches.Count == 1)
        {
            claims.Add(new Claim("ActiveBranchId", assignedBranches[0].Id.ToString()));
            claims.Add(new Claim("ActiveBranchName", assignedBranches[0].Name));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            return Results.Redirect(EnsureLocalUrl(returnUrl, "/"));
        }
        else if (assignedBranches.Count > 1)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            return Results.Redirect($"/select-branch{(string.IsNullOrEmpty(returnUrl) ? "" : "?returnUrl=" + Uri.EscapeDataString(EnsureLocalUrl(returnUrl, "/")))}");
        }
        else
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
            return Results.Redirect(EnsureLocalUrl(returnUrl, "/"));
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
