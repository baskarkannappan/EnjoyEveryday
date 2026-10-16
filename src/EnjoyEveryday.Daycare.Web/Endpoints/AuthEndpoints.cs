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

        group.MapGet("/dev-login", async (HttpContext context, IDbConnectionFactory db, [FromQuery] string? returnUrl) =>
        {
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsDevelopment())
            {
                return Results.Redirect("/login");
            }

            using var conn = await db.CreateConnectionAsync();
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT id, email, tenant_id FROM users WHERE email = 'admin@littlestars.com' UNION ALL SELECT id, email, tenant_id FROM users LIMIT 1");
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
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
                return Results.Redirect(returnUrl ?? "/");
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

            // 1. Check if external login is already linked
            var userId = await conn.ExecuteScalarAsync<Guid?>(
                "SELECT user_id FROM user_external_logins WHERE provider = 'Google' AND provider_key = @ProviderKey",
                new { ProviderKey = providerKey });

            if (userId == null)
            {
                // 2. Check if user already exists with matching email
                var existingUser = await conn.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT id, tenant_id, is_active FROM users WHERE normalized_email = UPPER(@Email) LIMIT 1",
                    new { Email = email });

                if (existingUser != null)
                {
                    userId = (Guid)existingUser.id;

                    // Atomically link account
                    await conn.ExecuteAsync(@"
                        INSERT INTO user_external_logins (provider, provider_key, user_id, created_at)
                        VALUES ('Google', @ProviderKey, @UserId, NOW())
                        ON CONFLICT (provider, provider_key) DO NOTHING;
                        UPDATE users SET email_confirmed = true, updated_at = NOW() WHERE id = @UserId;
                    ", new { ProviderKey = providerKey, UserId = userId });
                }
                else
                {
                    // 3. User does not exist -> Auto-create user account
                    var config = context.RequestServices.GetRequiredService<IConfiguration>();
                    if (!Guid.TryParse(config["DevTenantId"], out var tenantId))
                        tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

                    if (!Guid.TryParse(config["DevOrganizationId"], out var orgId))
                        orgId = Guid.Parse("20000000-0000-0000-0000-000000000001");

                    userId = Guid.NewGuid();

                    await conn.ExecuteAsync(@"
                        INSERT INTO users (
                            id, tenant_id, email, normalized_email, email_confirmed,
                            first_name, last_name, display_name, password_hash, security_stamp, is_active, created_at, updated_at
                        ) VALUES (
                            @UserId, @TenantId, @Email, UPPER(@Email), TRUE,
                            @FirstName, @LastName, @DisplayName, '', encode(gen_random_bytes(32), 'hex'), TRUE, NOW(), NOW()
                        );
                        INSERT INTO user_external_logins (provider, provider_key, user_id, created_at)
                        VALUES ('Google', @ProviderKey, @UserId, NOW())
                        ON CONFLICT (provider, provider_key) DO NOTHING;
                    ", new { 
                        UserId = userId, 
                        TenantId = tenantId, 
                        Email = email, 
                        FirstName = givenName, 
                        LastName = surname, 
                        DisplayName = fullName, 
                        ProviderKey = providerKey 
                    });

                    // Ensure organization membership
                    await conn.ExecuteAsync(@"
                        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
                        VALUES (@UserId, @OrgId, @TenantId, 'Active', NOW(), NOW())
                        ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active';
                    ", new { UserId = userId, OrgId = orgId, TenantId = tenantId });

                    // Assign teacher role in Daycare Web
                    var teacherRoleId = await conn.ExecuteScalarAsync<Guid?>(@"
                        SELECT id FROM roles 
                        WHERE name IN ('DaycareTeacher', 'Teacher', 'ClassroomTeacher', 'OrganizationAdmin') 
                          AND (tenant_id = @TenantId OR tenant_id IS NULL)
                        ORDER BY CASE WHEN name = 'DaycareTeacher' THEN 1 WHEN name = 'Teacher' THEN 2 ELSE 3 END 
                        LIMIT 1", new { TenantId = tenantId });

                    if (teacherRoleId.HasValue)
                    {
                        await conn.ExecuteAsync(@"
                            INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
                            VALUES (@UserId, @RoleId, @OrgId, @TenantId)
                            ON CONFLICT DO NOTHING;
                        ", new { UserId = userId, RoleId = teacherRoleId.Value, OrgId = orgId, TenantId = tenantId });
                    }

                    // Ensure teacher profile exists
                    await conn.ExecuteAsync(@"
                        INSERT INTO teachers (id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at)
                        SELECT gen_random_uuid(), @TenantId, @UserId, @FirstName, @LastName, @DisplayName, 'Active', NOW(), NOW()
                        WHERE NOT EXISTS (SELECT 1 FROM teachers WHERE user_id = @UserId);
                    ", new { TenantId = tenantId, UserId = userId, FirstName = givenName, LastName = surname, DisplayName = fullName });
                }
            }

            // 4. We have a linked user ID. Ensure the account is active.
            var user = await conn.QuerySingleOrDefaultAsync<(Guid Id, Guid TenantId, bool IsActive)?>(
                "SELECT id as Id, tenant_id as TenantId, is_active as IsActive FROM users WHERE id = @UserId", 
                new { UserId = userId });

            if (user == null || !user.Value.IsActive)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=account_disabled");
            }

            // 5. Rebuild the claims explicitly from our trusted DB
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Value.Id.ToString()),
                new Claim(ClaimTypes.Email, email ?? string.Empty),
                new Claim("TenantId", user.Value.TenantId.ToString())
            };

            var memberships = (await conn.QueryAsync<Guid>(
                "SELECT organization_id FROM organization_memberships WHERE user_id = @UserId AND status = 'Active'",
                new { UserId = user.Value.Id })).ToList();

            if (memberships.Count == 0)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=no_organizations");
            }

            Guid? activeOrgId = memberships.Count == 1 ? memberships[0] : null;

            await BuildAuthClaimsAsync(conn, claims, user.Value.Id, activeOrgId);

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
        }).DisableAntiforgery();

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
                "google_not_configured" => "Google Authentication is not yet configured. Please set Authentication:Google:ClientId in appsettings.json or use dev login.",
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
              WHERE ur.user_id = @UserId AND r.scope_type = 'Platform' AND r.tenant_id IS NULL
              UNION
              SELECT r.name 
              FROM user_organization_roles uor JOIN roles r ON uor.role_id = r.id 
              JOIN organization_memberships om ON om.user_id = uor.user_id AND om.organization_id = uor.organization_id AND om.status = 'Active'
              WHERE uor.user_id = @UserId AND uor.organization_id = @ActiveOrgId",
            new { UserId = userId, ActiveOrgId = activeOrgId });

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var permissions = await conn.QueryAsync<string>(
            @"SELECT p.name 
              FROM user_roles ur 
              JOIN roles gr ON gr.id = ur.role_id AND gr.scope_type = 'Platform' AND gr.tenant_id IS NULL
              JOIN role_permissions rp ON ur.role_id = rp.role_id 
              JOIN permissions p ON rp.permission_id = p.id 
              WHERE ur.user_id = @UserId
              UNION
              SELECT p.name 
              FROM user_organization_roles uor 
              JOIN organization_memberships om ON om.user_id = uor.user_id AND om.organization_id = uor.organization_id AND om.status = 'Active'
              JOIN role_permissions rp ON uor.role_id = rp.role_id 
              JOIN permissions p ON rp.permission_id = p.id 
              WHERE uor.user_id = @UserId AND uor.organization_id = @ActiveOrgId",
            new { UserId = userId, ActiveOrgId = activeOrgId });

        foreach (var perm in permissions)
            claims.Add(new Claim("Permission", perm));
    }
}
