using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class PlatformAdminRepository : IPlatformAdminRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PlatformAdminRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<List<PlatformDbOrganizationRecord>> GetOrganizationsOverviewAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string dbSql = @"
            SELECT o.id AS Id, o.tenant_id AS TenantId, o.name AS Name,
                   o.location AS Location, o.is_active AS IsActive, o.created_at AS CreatedAt,
                   COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS BranchCount,
                   (SELECT u.email FROM organization_memberships om JOIN users u ON om.user_id = u.id WHERE om.organization_id = o.id AND om.status = 'Active' ORDER BY om.created_at LIMIT 1) AS AdminEmail
            FROM organizations o
            ORDER BY o.name";

        var results = await conn.QueryAsync<PlatformDbOrganizationRecord>(new CommandDefinition(dbSql, cancellationToken: ct));
        return results.ToList();
    }

    public async Task ProvisionOrganizationAsync(Guid id, string name, string location, string adminEmail, int branchCount = 1, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // Every organization provisioned on the platform is an independent tenant
        Guid orgTenantId = id;
        var rawSlug = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        var slug = string.IsNullOrWhiteSpace(rawSlug) ? $"org-{id.ToString()[..8]}" : rawSlug;

        var existingSlugCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(1) FROM tenants WHERE slug = @Slug", new { Slug = slug }, cancellationToken: ct));

        if (existingSlugCount > 0)
        {
            slug = $"{slug}-{id.ToString()[..4]}";
        }

        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO tenants (id, name, slug) 
            VALUES (@TenantId, @Name, @Slug) 
            ON CONFLICT (id) DO NOTHING",
            new { TenantId = orgTenantId, Name = name, Slug = slug },
            cancellationToken: ct));

        // Check if admin email belongs to an existing user
        var existingUser = await conn.QuerySingleOrDefaultAsync<(Guid Id, Guid TenantId)?>(
            new CommandDefinition(
                "SELECT id as Id, tenant_id as TenantId FROM users WHERE LOWER(email) = LOWER(@Email) OR normalized_email = UPPER(@Email) LIMIT 1",
                new { Email = adminEmail.Trim() },
                cancellationToken: ct));

        Guid adminUserId;
        if (existingUser.HasValue)
        {
            adminUserId = existingUser.Value.Id;
            // If the user was previously placed in the default seed tenant, update them to this tenant
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE users SET tenant_id = @TenantId WHERE id = @UserId AND tenant_id = '10000000-0000-0000-0000-000000000001'",
                new { TenantId = orgTenantId, UserId = adminUserId },
                cancellationToken: ct));
        }
        else
        {
            // Create new admin user
            adminUserId = Guid.NewGuid();
            var derivedAdminName = DeriveDisplayNameFromEmail(adminEmail.Trim());
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
                VALUES (@UserId, @TenantId, @Email, UPPER(@Email), TRUE, 'Admin', @DisplayName, @DisplayName, TRUE)
                ON CONFLICT (normalized_email) DO NOTHING",
                new { UserId = adminUserId, TenantId = orgTenantId, Email = adminEmail.Trim(), DisplayName = derivedAdminName },
                cancellationToken: ct));
        }

        // Insert Organization
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO organizations (id, tenant_id, name, location, is_active)
            VALUES (@Id, @TenantId, @Name, @Location, TRUE)
            ON CONFLICT (id) DO NOTHING",
            new { Id = id, TenantId = orgTenantId, Name = name, Location = location },
            cancellationToken: ct));

        // Provision initial branches based on requested branchCount
        int branchesToCreate = Math.Max(1, branchCount);
        for (int i = 1; i <= branchesToCreate; i++)
        {
            var branchId = Guid.NewGuid();
            string branchName = i == 1 ? "Main Campus" : (i == 2 ? "Annex Campus" : $"Campus {i}");
            string branchLoc = !string.IsNullOrWhiteSpace(location) ? location : "Main Branch";

            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
                VALUES (@BranchId, @TenantId, @OrgId, @Name, @Location, TRUE)
                ON CONFLICT (id) DO NOTHING",
                new { BranchId = branchId, TenantId = orgTenantId, OrgId = id, Name = branchName, Location = branchLoc },
                cancellationToken: ct));
        }

        // Link admin user membership to this organization
        if (adminUserId != Guid.Empty)
        {
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
                VALUES (@UserId, @OrgId, @TenantId, 'Active')
                ON CONFLICT (user_id, organization_id) DO NOTHING",
                new { UserId = adminUserId, OrgId = id, TenantId = orgTenantId },
                cancellationToken: ct));

            // Assign OrganizationAdmin role
            var roleId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                @"SELECT id FROM roles 
                  WHERE name IN ('OrganizationAdmin', 'OrganizationOwner') 
                    AND (tenant_id = @TenantId OR tenant_id IS NULL) 
                  ORDER BY tenant_id NULLS LAST LIMIT 1",
                new { TenantId = orgTenantId },
                cancellationToken: ct));

            if (roleId.HasValue)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
                    VALUES (@UserId, @RoleId, @OrgId, @TenantId)
                    ON CONFLICT (user_id, role_id, organization_id) DO NOTHING",
                    new { UserId = adminUserId, RoleId = roleId.Value, OrgId = id, TenantId = orgTenantId },
                    cancellationToken: ct));
            }
        }
    }

    public async Task UpdateOrganizationDetailsAsync(Guid id, string name, string? location, string adminEmail, int branchCount, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // Update organization name & location
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE organizations 
            SET name = @Name, location = @Location, updated_at = NOW() 
            WHERE id = @Id",
            new { Id = id, Name = name, Location = location },
            cancellationToken: ct));

        // Update corresponding tenant
        await conn.ExecuteAsync(new CommandDefinition(@"
            UPDATE tenants 
            SET name = @Name, updated_at = NOW() 
            WHERE id = @Id",
            new { Id = id, Name = name },
            cancellationToken: ct));

        // Check tenant id for this organization
        var tenantId = await conn.ExecuteScalarAsync<Guid?>(
            new CommandDefinition("SELECT tenant_id FROM organizations WHERE id = @Id", new { Id = id }, cancellationToken: ct)) ?? id;

        // Check existing branch count
        var currentBranches = (await conn.QueryAsync<dynamic>(
            new CommandDefinition("SELECT id, name FROM branches WHERE organization_id = @Id ORDER BY created_at", new { Id = id }, cancellationToken: ct))).ToList();

        if (currentBranches.Count == 0)
        {
            // Provision at least the main branch
            int count = Math.Max(1, branchCount);
            for (int i = 1; i <= count; i++)
            {
                string bName = i == 1 ? "Main Campus" : (i == 2 ? "Annex Campus" : $"Campus {i}");
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
                    VALUES (gen_random_uuid(), @TenantId, @OrgId, @Name, @Location, TRUE)",
                    new { TenantId = tenantId, OrgId = id, Name = bName, Location = location ?? "Branch Location" },
                    cancellationToken: ct));
            }
        }
        else if (currentBranches.Count < branchCount)
        {
            // Add additional branches up to branchCount
            for (int i = currentBranches.Count + 1; i <= branchCount; i++)
            {
                string bName = i == 2 ? "Annex Campus" : $"Campus {i}";
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
                    VALUES (gen_random_uuid(), @TenantId, @OrgId, @Name, @Location, TRUE)",
                    new { TenantId = tenantId, OrgId = id, Name = bName, Location = location ?? "Branch Location" },
                    cancellationToken: ct));
            }
        }

        // Update administrator email and display name if provided
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var trimmedEmail = adminEmail.Trim();
            var derivedName = DeriveDisplayNameFromEmail(trimmedEmail);

            // Check if a user with this email already exists in the system
            var existingUserId = await conn.ExecuteScalarAsync<Guid?>(
                new CommandDefinition(@"
                    SELECT id FROM users 
                    WHERE normalized_email = UPPER(@Email) 
                    LIMIT 1", new { Email = trimmedEmail }, cancellationToken: ct));

            Guid adminUserId;

            if (existingUserId.HasValue)
            {
                adminUserId = existingUserId.Value;
                // Ensure user is active
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE users 
                    SET is_active = TRUE,
                        display_name = COALESCE(NULLIF(TRIM(display_name), ''), @DisplayName),
                        updated_at = NOW() 
                    WHERE id = @UserId",
                    new { UserId = adminUserId, DisplayName = derivedName },
                    cancellationToken: ct));
            }
            else
            {
                // Find current active administrator linked to this organization
                var currentUserId = await conn.ExecuteScalarAsync<Guid?>(
                    new CommandDefinition(@"
                        SELECT om.user_id 
                        FROM organization_memberships om
                        JOIN users u ON u.id = om.user_id
                        WHERE om.organization_id = @OrgId AND om.status = 'Active'
                        ORDER BY om.created_at
                        LIMIT 1", new { OrgId = id }, cancellationToken: ct));

                if (currentUserId.HasValue)
                {
                    adminUserId = currentUserId.Value;
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        UPDATE users 
                        SET email = @Email, 
                            normalized_email = UPPER(@Email), 
                            display_name = @DisplayName,
                            updated_at = NOW() 
                        WHERE id = @UserId",
                        new { UserId = adminUserId, Email = trimmedEmail, DisplayName = derivedName },
                        cancellationToken: ct));
                }
                else
                {
                    adminUserId = Guid.NewGuid();
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
                        VALUES (@UserId, @TenantId, @Email, UPPER(@Email), TRUE, 'Admin', @DisplayName, @DisplayName, TRUE)
                        ON CONFLICT (normalized_email) DO UPDATE SET updated_at = NOW()",
                        new { UserId = adminUserId, TenantId = tenantId, Email = trimmedEmail, DisplayName = derivedName },
                        cancellationToken: ct));
                }
            }

            // Remove any other stale/dummy admin assignments for this organization
            await conn.ExecuteAsync(new CommandDefinition(@"
                DELETE FROM user_organization_roles 
                WHERE organization_id = @OrgId AND user_id != @UserId;
                DELETE FROM organization_memberships 
                WHERE organization_id = @OrgId AND user_id != @UserId;",
                new { OrgId = id, UserId = adminUserId },
                cancellationToken: ct));

            // Link administrator to this organization
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
                VALUES (@UserId, @OrgId, @TenantId, 'Active')
                ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active', tenant_id = @TenantId",
                new { UserId = adminUserId, OrgId = id, TenantId = tenantId },
                cancellationToken: ct));

            // Ensure roles OrganizationOwner & OrganizationAdmin are assigned to this user
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
                SELECT @UserId, r.id, @OrgId, @TenantId
                FROM roles r
                WHERE r.tenant_id = @TenantId AND r.name IN ('OrganizationOwner', 'OrganizationAdmin')
                ON CONFLICT (user_id, role_id, organization_id) DO NOTHING",
                new { UserId = adminUserId, OrgId = id, TenantId = tenantId },
                cancellationToken: ct));
        }
    }

    public async Task<List<PlatformDbBranchRecord>> GetAllBranchesAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT 
                b.id AS Id,
                b.organization_id AS OrganizationId,
                b.name AS Name,
                b.location AS Location,
                b.is_active AS IsActive,
                b.created_at AS CreatedAt,
                o.name AS OrganizationName,
                COALESCE(b.location, o.location, 'Location unspecified') AS EffectiveLocation,
                COALESCE((SELECT COUNT(*) FROM classrooms c WHERE c.branch_id = b.id), 0)::int AS ClassroomsCount,
                COALESCE((SELECT COUNT(*) FROM children ch JOIN classrooms c ON ch.classroom_id = c.id WHERE c.branch_id = b.id), 0)::int AS ChildrenCount
            FROM branches b
            JOIN organizations o ON b.organization_id = o.id
            ORDER BY o.name, b.name";

        var list = await conn.QueryAsync<PlatformDbBranchRecord>(new CommandDefinition(sql, cancellationToken: ct));
        return list.ToList();
    }

    public async Task<PlatformDbBranchRecord> AddBranchAsync(Guid organizationId, string name, string? location, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var tenantId = await conn.ExecuteScalarAsync<Guid?>(
            new CommandDefinition("SELECT tenant_id FROM organizations WHERE id = @Id", new { Id = organizationId }, cancellationToken: ct));

        if (!tenantId.HasValue)
        {
            tenantId = organizationId;
        }

        var branchId = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
            VALUES (@BranchId, @TenantId, @OrgId, @Name, @Location, TRUE)",
            new { BranchId = branchId, TenantId = tenantId.Value, OrgId = organizationId, Name = name, Location = location },
            cancellationToken: ct));

        var orgName = await conn.ExecuteScalarAsync<string>(
            new CommandDefinition("SELECT name FROM organizations WHERE id = @Id", new { Id = organizationId }, cancellationToken: ct)) ?? "Organization";

        return new PlatformDbBranchRecord
        {
            Id = branchId,
            OrganizationId = organizationId,
            Name = name,
            Location = location,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            OrganizationName = orgName,
            EffectiveLocation = location ?? "Location unspecified",
            ClassroomsCount = 0,
            ChildrenCount = 0
        };
    }

    public async Task<List<PlatformDbAdminUserRecord>> GetOrganizationAdminsAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT DISTINCT ON (o.id, COALESCE(u.id, '00000000-0000-0000-0000-000000000000'::uuid))
                COALESCE(u.id, gen_random_uuid()) AS UserId,
                CASE 
                    WHEN u.display_name IS NOT NULL AND TRIM(u.display_name) != '' AND TRIM(u.display_name) != o.name THEN TRIM(u.display_name)
                    WHEN TRIM(CONCAT(u.first_name, ' ', u.last_name)) != '' AND TRIM(CONCAT(u.first_name, ' ', u.last_name)) != 'Admin' AND TRIM(CONCAT(u.first_name, ' ', u.last_name)) != o.name THEN TRIM(CONCAT(u.first_name, ' ', u.last_name))
                    WHEN u.email IS NOT NULL AND u.email != '' THEN INITCAP(SPLIT_PART(SPLIT_PART(u.email, '@', 1), '.', 1))
                    ELSE 'Daycare Director'
                END AS AdminName,
                COALESCE(u.email, 'admin@daycare.org') AS AdminEmail,
                o.id AS OrganizationId,
                o.name AS OrganizationName,
                COALESCE(r.name, 'Organization Admin') AS RoleName,
                CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END AS AccountStatus
            FROM organizations o
            LEFT JOIN organization_memberships om ON om.organization_id = o.id AND om.status = 'Active'
            LEFT JOIN users u ON u.id = om.user_id
            LEFT JOIN user_organization_roles uor ON uor.user_id = u.id AND uor.organization_id = o.id
            LEFT JOIN roles r ON r.id = uor.role_id
            ORDER BY o.id, COALESCE(u.id, '00000000-0000-0000-0000-000000000000'::uuid), 
                     CASE WHEN r.name = 'OrganizationOwner' THEN 1 WHEN r.name = 'OrganizationAdmin' THEN 2 ELSE 3 END";

        var list = await conn.QueryAsync<PlatformDbAdminUserRecord>(new CommandDefinition(sql, cancellationToken: ct));
        return list.ToList();
    }

    private static string DeriveDisplayNameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "Daycare Director";
        var localPart = email.Split('@')[0];
        var parts = localPart.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !int.TryParse(p, out _))
            .Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..].ToLowerInvariant() : ""))
            .ToList();

        return parts.Count > 0 ? string.Join(" ", parts) : "Daycare Director";
    }
}
