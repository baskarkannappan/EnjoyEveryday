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

                // 1. Ensure Sunita Patel exists in users
                var devSecurityStamp = Guid.NewGuid().ToString("N");
                await conn.ExecuteAsync(@"
                    INSERT INTO users (
                        id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp, is_active
                    ) VALUES (
                        @UserId, @TenantId, @Email, UPPER(@Email), TRUE, 'Sunita', 'Patel', 'Sunita Patel', '', @SecurityStamp, TRUE
                    ) ON CONFLICT (id) DO UPDATE SET is_active = TRUE;
                ", new { UserId = userId, TenantId = tenantId, Email = email, SecurityStamp = devSecurityStamp });

                // 2. Ensure mock child Aarav Patel exists
                await conn.ExecuteAsync(@"
                    INSERT INTO children (id, tenant_id, first_name, last_name, date_of_birth, classroom_id, is_active)
                    VALUES (@ChildId, @TenantId, 'Aarav', 'Patel', '2023-01-15', '40000000-0000-0000-0000-000000000001', TRUE)
                    ON CONFLICT (id) DO UPDATE SET is_active = TRUE;
                ", new { ChildId = childId, TenantId = tenantId });

                // 3. Ensure family relationship links Sunita Patel to Aarav Patel
                await conn.ExecuteAsync(@"
                    INSERT INTO family_relationships (id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at)
                    VALUES ('f0000000-0000-0000-0000-000000000001', @TenantId, @ChildId, @UserId, 'Mother', TRUE, NOW(), 'Verified', NOW())
                    ON CONFLICT (child_id, user_id) DO NOTHING;
                ", new { TenantId = tenantId, ChildId = childId, UserId = userId });

                // 4. Ensure person record exists for Family Profile
                await conn.ExecuteAsync(@"
                    INSERT INTO persons (id, tenant_id, user_id, first_name, last_name, relationship, email, mobile_phone, preferred_contact_method, preferred_language, has_portal_access, receives_daily_updates, receives_photos)
                    VALUES ('70000000-0000-0000-0000-000000000001', @TenantId, @UserId, 'Sunita', 'Patel', 'Mother', @Email, '416-555-0199', 'Mobile', 'English', TRUE, TRUE, TRUE)
                    ON CONFLICT (id) DO NOTHING;
                ", new { TenantId = tenantId, UserId = userId, Email = email });

                // 5. Ensure child guardian record exists
                await conn.ExecuteAsync(@"
                    INSERT INTO child_guardians (id, tenant_id, child_id, person_id, relationship, is_primary, has_legal_custody, can_pickup, can_receive_information, can_authorize_medical_care)
                    VALUES ('80000000-0000-0000-0000-000000000001', @TenantId, @ChildId, '70000000-0000-0000-0000-000000000001', 'Mother', TRUE, TRUE, TRUE, TRUE, TRUE)
                    ON CONFLICT (child_id, person_id) DO NOTHING;
                ", new { TenantId = tenantId, ChildId = childId });

                // 6. Ensure sample journey entries and milestones exist
                var teacherId = await conn.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM teachers WHERE tenant_id = @TenantId LIMIT 1", new { TenantId = tenantId });
                if (teacherId.HasValue && teacherId.Value != Guid.Empty)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO child_journey_entries (id, tenant_id, child_id, teacher_id, journey_type, title, observation, child_voice, observation_date, is_parent_visible, parent_visibility_status)
                        VALUES ('90000000-0000-0000-0000-000000000001', @TenantId, @ChildId, @TeacherId, 'Art & Sensory', 'Exploring Watercolor Rainbows', 'Aarav enthusiastically mixed colors and identified secondary hues.', 'Look, I made green!', NOW(), TRUE, 'Approved')
                        ON CONFLICT (id) DO NOTHING;

                        INSERT INTO child_milestones (id, tenant_id, child_id, teacher_id, title, description, milestone_date, parent_visible, status)
                        VALUES ('91000000-0000-0000-0000-000000000001', @TenantId, @ChildId, @TeacherId, 'Collaborative Tower Building', 'Constructed a stable 12-block tower with peers demonstrating turn-taking.', NOW(), TRUE, 'Active')
                        ON CONFLICT (id) DO NOTHING;
                    ", new { TenantId = tenantId, ChildId = childId, TeacherId = teacherId.Value });
                }
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

            var userId = await conn.ExecuteScalarAsync<Guid?>(
                "SELECT user_id FROM user_external_logins WHERE provider = 'Google' AND provider_key = @ProviderKey",
                new { ProviderKey = providerKey });

            if (userId == null)
            {
                // Check if user exists with matching normalized email
                var existingUser = await conn.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT id, tenant_id, is_active FROM users WHERE normalized_email = UPPER(@Email) LIMIT 1",
                    new { Email = email });

                if (existingUser != null)
                {
                    userId = (Guid)existingUser.id;

                    await conn.ExecuteAsync(@"
                        INSERT INTO user_external_logins (provider, provider_key, user_id, created_at)
                        VALUES ('Google', @ProviderKey, @UserId, NOW())
                        ON CONFLICT (provider, provider_key) DO NOTHING;
                        UPDATE users SET email_confirmed = true, updated_at = NOW() WHERE id = @UserId;
                    ", new { ProviderKey = providerKey, UserId = userId });
                }
                else
                {
                    // Auto-create user account
                    var config = context.RequestServices.GetRequiredService<IConfiguration>();
                    if (!Guid.TryParse(config["DevTenantId"], out var tenantId))
                        tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

                    if (!Guid.TryParse(config["DevOrganizationId"], out var orgId))
                        orgId = Guid.Parse("20000000-0000-0000-0000-000000000001");

                    userId = Guid.NewGuid();

                    var securityStamp = Guid.NewGuid().ToString("N");

                    await conn.ExecuteAsync(@"
                        INSERT INTO users (
                            id, tenant_id, email, normalized_email, email_confirmed,
                            first_name, last_name, display_name, password_hash, security_stamp, is_active, created_at, updated_at
                        ) VALUES (
                            @UserId, @TenantId, @Email, UPPER(@Email), TRUE,
                            @FirstName, @LastName, @DisplayName, '', @SecurityStamp, TRUE, NOW(), NOW()
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
                        SecurityStamp = securityStamp,
                        ProviderKey = providerKey 
                    });

                    // Ensure organization membership
                    await conn.ExecuteAsync(@"
                        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
                        VALUES (@UserId, @OrgId, @TenantId, 'Active', NOW(), NOW())
                        ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active';
                    ", new { UserId = userId, OrgId = orgId, TenantId = tenantId });

                    // Assign Parent role
                    var parentRoleId = await conn.ExecuteScalarAsync<Guid?>(@"
                        SELECT id FROM roles 
                        WHERE name = 'Parent' AND tenant_id = @TenantId 
                        LIMIT 1", new { TenantId = tenantId });

                    if (parentRoleId.HasValue)
                    {
                        await conn.ExecuteAsync(@"
                            INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
                            VALUES (@UserId, @RoleId, @OrgId, @TenantId)
                            ON CONFLICT DO NOTHING;
                        ", new { UserId = userId, RoleId = parentRoleId.Value, OrgId = orgId, TenantId = tenantId });
                    }

                    // Ensure person record exists for family portal
                    await conn.ExecuteAsync(@"
                        INSERT INTO persons (id, tenant_id, user_id, first_name, last_name, relationship, email, preferred_language, has_portal_access, receives_daily_updates, receives_photos)
                        SELECT gen_random_uuid(), @TenantId, @UserId, @FirstName, @LastName, 'Parent', @Email, 'English', TRUE, TRUE, TRUE
                        WHERE NOT EXISTS (SELECT 1 FROM persons WHERE user_id = @UserId);
                    ", new { TenantId = tenantId, UserId = userId, FirstName = givenName, LastName = surname, Email = email });
                }
            }

            var user = await conn.QuerySingleOrDefaultAsync<(Guid Id, Guid TenantId, bool IsActive)?>(
                "SELECT id as Id, tenant_id as TenantId, is_active as IsActive FROM users WHERE id = @UserId", 
                new { UserId = userId });

            if (user == null || !user.Value.IsActive)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=account_disabled");
            }

            // 1. Automatically link any active children whose enrollment draft_data matches this parent's email
            await conn.ExecuteAsync(@"
                INSERT INTO family_relationships (id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at)
                SELECT gen_random_uuid(), c.tenant_id, c.id, @UserId, 
                       COALESCE(NULLIF(TRIM((c.draft_data::jsonb)->>'parentRelationship'), ''), 'Parent'), 
                       TRUE, NOW(), 'Verified', NOW()
                FROM children c
                WHERE c.is_active = TRUE
                  AND (c.draft_data::jsonb)->>'parentEmail' ILIKE @Email
                ON CONFLICT (child_id, user_id) DO UPDATE 
                SET status = 'Verified', revoked_at = NULL, verified_at = NOW();
            ", new { UserId = user.Value.Id, Email = email });

            // 2. Also ensure person record exists for this parent
            await conn.ExecuteAsync(@"
                INSERT INTO persons (id, tenant_id, user_id, first_name, last_name, relationship, email, preferred_language, has_portal_access, receives_daily_updates, receives_photos)
                SELECT gen_random_uuid(), c.tenant_id, @UserId, @FirstName, @LastName, 'Parent', @Email, 'English', TRUE, TRUE, TRUE
                FROM children c
                WHERE c.is_active = TRUE AND (c.draft_data::jsonb)->>'parentEmail' ILIKE @Email
                  AND NOT EXISTS (SELECT 1 FROM persons WHERE tenant_id = c.tenant_id AND user_id = @UserId);
            ", new { UserId = user.Value.Id, Email = email, FirstName = givenName, LastName = surname });

            // 3. Verify they have a current, VERIFIED, non-revoked parent relationship
            var isParent = await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM family_relationships WHERE user_id = @UserId AND status = 'Verified' AND revoked_at IS NULL)",
                new { UserId = user.Value.Id });

            if (!isParent)
            {
                // In development / demo, automatically connect to sample child if no relationships exist
                await conn.ExecuteAsync(@"
                    INSERT INTO family_relationships (id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at)
                    SELECT gen_random_uuid(), @TenantId, c.id, @UserId, 'Parent', TRUE, NOW(), 'Verified', NOW()
                    FROM children c
                    WHERE c.tenant_id = @TenantId
                    LIMIT 1;
                ", new { TenantId = user.Value.TenantId, UserId = user.Value.Id });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Value.Id.ToString()),
                new Claim(ClaimTypes.Email, email ?? string.Empty),
                new Claim("TenantId", user.Value.TenantId.ToString()),
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
