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
                   COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS BranchCount
            FROM organizations o
            ORDER BY o.name";

        var results = await conn.QueryAsync<PlatformDbOrganizationRecord>(new CommandDefinition(dbSql, cancellationToken: ct));
        return results.ToList();
    }

    public async Task ProvisionOrganizationAsync(Guid id, string name, string location, string adminEmail, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // Check if admin email belongs to an existing user
        var existingUser = await conn.QuerySingleOrDefaultAsync<(Guid Id, Guid TenantId)?>(
            new CommandDefinition(
                "SELECT id as Id, tenant_id as TenantId FROM users WHERE LOWER(email) = LOWER(@Email) OR normalized_email = UPPER(@Email) LIMIT 1",
                new { Email = adminEmail.Trim() },
                cancellationToken: ct));

        Guid orgTenantId;
        Guid? adminUserId = null;

        if (existingUser.HasValue)
        {
            orgTenantId = existingUser.Value.TenantId;
            adminUserId = existingUser.Value.Id;
        }
        else
        {
            orgTenantId = id;
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

            // Create new admin user
            adminUserId = Guid.NewGuid();
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
                VALUES (@UserId, @TenantId, @Email, UPPER(@Email), TRUE, 'Admin', @OrgName, @OrgName, TRUE)
                ON CONFLICT (normalized_email) DO NOTHING",
                new { UserId = adminUserId.Value, TenantId = orgTenantId, Email = adminEmail.Trim(), OrgName = name },
                cancellationToken: ct));
        }

        // Insert Organization
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO organizations (id, tenant_id, name, location, is_active)
            VALUES (@Id, @TenantId, @Name, @Location, TRUE)
            ON CONFLICT (id) DO NOTHING",
            new { Id = id, TenantId = orgTenantId, Name = name, Location = location },
            cancellationToken: ct));

        // Insert Default Main Campus Branch
        var branchId = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
            VALUES (@BranchId, @TenantId, @OrgId, 'Main Campus', @Location, TRUE)
            ON CONFLICT (id) DO NOTHING",
            new { BranchId = branchId, TenantId = orgTenantId, OrgId = id, Location = location },
            cancellationToken: ct));

        // Link admin user membership to this organization
        if (adminUserId.HasValue)
        {
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
                VALUES (@UserId, @OrgId, @TenantId, 'Active')
                ON CONFLICT (user_id, organization_id) DO NOTHING",
                new { UserId = adminUserId.Value, OrgId = id, TenantId = orgTenantId },
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
                    new { UserId = adminUserId.Value, RoleId = roleId.Value, OrgId = id, TenantId = orgTenantId },
                    cancellationToken: ct));
            }
        }
    }
}
