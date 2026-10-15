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

        group.MapGet("/login", async (HttpContext context, IDbConnectionFactory db, [FromQuery] string? returnUrl, [FromQuery] string? inviteToken) =>
        {
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
            {
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
                    await conn.ExecuteAsync(@"
                        INSERT INTO users (
                            id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp, is_active
                        ) VALUES (
                            @UserId, @TenantId, @Email, UPPER(@Email), TRUE, 'Sunita', 'Patel', 'Sunita Patel', '', encode(gen_random_bytes(32), 'hex'), TRUE
                        ) ON CONFLICT (id) DO UPDATE SET is_active = TRUE;
                    ", new { UserId = userId, TenantId = tenantId, Email = email });

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

            var userId = await conn.ExecuteScalarAsync<Guid?>(
                "SELECT user_id FROM user_external_logins WHERE provider = 'Google' AND provider_key = @ProviderKey",
                new { ProviderKey = providerKey });

            if (userId == null)
            {
                if (string.IsNullOrEmpty(inviteToken))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect("/auth/access-denied?reason=not_linked");
                }

                var invitedUser = await conn.QuerySingleOrDefaultAsync<Guid?>(
                    "SELECT id FROM users WHERE email = @Email AND security_stamp = @Token AND is_active = true",
                    new { Email = email, Token = inviteToken });

                if (invitedUser == null)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect("/auth/access-denied?reason=invalid_invite");
                }

                userId = invitedUser.Value;

                await conn.ExecuteAsync(@"
                    INSERT INTO user_external_logins (provider, provider_key, user_id) VALUES ('Google', @ProviderKey, @UserId);
                    UPDATE users SET security_stamp = encode(gen_random_bytes(32), 'hex'), email_confirmed = true WHERE id = @UserId;
                ", new { ProviderKey = providerKey, UserId = userId });
            }

            var user = await conn.QuerySingleOrDefaultAsync<(Guid Id, Guid TenantId, bool IsActive)?>(
                "SELECT id as Id, tenant_id as TenantId, is_active as IsActive FROM users WHERE id = @UserId", 
                new { UserId = userId });

            if (user == null || !user.Value.IsActive)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=account_disabled");
            }

            // Verify they have a current, VERIFIED, non-revoked parent relationship in this tenant
            var isParent = await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM family_relationships WHERE user_id = @UserId AND tenant_id = @TenantId AND status = 'Verified' AND revoked_at IS NULL)",
                new { UserId = user.Value.Id, TenantId = user.Value.TenantId });

            if (!isParent)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/auth/access-denied?reason=not_a_parent");
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
            return Results.Redirect("/auth/login");
        });

        group.MapGet("/access-denied", ([FromQuery] string? reason) => 
        {
            var message = reason switch 
            {
                "not_linked" => "Your account is not linked. Please use the invitation link sent by your daycare administrator.",
                "invalid_invite" => "The invitation link is invalid or expired.",
                "account_disabled" => "Your account is disabled. Contact your daycare administrator.",
                "not_a_parent" => "No active guardian relationship was found for this account. Contact your daycare administrator.",
                _ => "Access Denied."
            };
            return Results.Text(message, "text/plain");
        });
    }
}
