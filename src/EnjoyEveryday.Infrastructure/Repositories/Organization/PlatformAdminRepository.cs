using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
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
        var results = await conn.QueryFunctionAsync<PlatformDbOrganizationRecord>(
            "fn_get_platform_organizations_overview",
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task ProvisionOrganizationAsync(Guid id, string name, string location, string adminEmail, int branchCount = 1, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_provision_platform_organization",
            new
            {
                OrgId = id,
                Name = name,
                Location = location,
                AdminEmail = adminEmail,
                BranchCount = branchCount
            },
            cancellationToken: ct);
    }

    public async Task UpdateOrganizationDetailsAsync(Guid id, string name, string? location, string adminEmail, int branchCount, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_platform_organization_details",
            new
            {
                Id = id,
                Name = name,
                Location = location,
                AdminEmail = adminEmail,
                BranchCount = branchCount
            },
            cancellationToken: ct);
    }

    public async Task<List<PlatformDbBranchRecord>> GetAllBranchesAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryFunctionAsync<PlatformDbBranchRecord>(
            "fn_get_platform_all_branches",
            cancellationToken: ct);
        return list.ToList();
    }

    public async Task<PlatformDbBranchRecord> AddBranchAsync(Guid organizationId, string name, string? location, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var branchId = Guid.NewGuid();
        var record = await conn.QuerySingleFunctionAsync<PlatformDbBranchRecord>(
            "fn_add_platform_branch",
            new
            {
                BranchId = branchId,
                OrganizationId = organizationId,
                Name = name,
                Location = location
            },
            cancellationToken: ct);
        return record ?? new PlatformDbBranchRecord
        {
            Id = branchId,
            OrganizationId = organizationId,
            Name = name,
            Location = location
        };
    }

    public async Task<List<PlatformDbAdminUserRecord>> GetOrganizationAdminsAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryFunctionAsync<PlatformDbAdminUserRecord>(
            "fn_get_platform_organization_admins",
            cancellationToken: ct);
        return list.ToList();
    }

    public async Task<int> GetPlatformAdminCountAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<int>(
            "fn_get_platform_admin_count",
            cancellationToken: ct);
    }

    public async Task CreateAdminUserAsync(string email, string firstName, string lastName, string password, string roleName, Guid? orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_admin_user",
            new
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Password = password,
                RoleName = roleName,
                OrgId = orgId
            },
            cancellationToken: ct);
    }

    public async Task UpdateAdminUserAsync(Guid userId, string email, string firstName, string lastName, string? password, string roleName, string accountStatus, Guid? orgId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_admin_user",
            new
            {
                UserId = userId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Password = password ?? string.Empty,
                RoleName = roleName,
                AccountStatus = accountStatus,
                OrgId = orgId
            },
            cancellationToken: ct);
    }

    public async Task DeleteAdminUserAsync(Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_delete_admin_user",
            new
            {
                UserId = userId
            },
            cancellationToken: ct);
    }

    public async Task<PlatformOrganizationInvitationRecord?> CreateOrganizationInvitationAsync(Guid organizationId, string adminEmail, string token, int expiresDays = 7, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM fn_create_organization_invitation(@p_organization_id, @p_admin_email, @p_token, @p_expires_days)",
            new { p_organization_id = organizationId, p_admin_email = adminEmail, p_token = token, p_expires_days = expiresDays });

        if (row == null) return null;
        return new PlatformOrganizationInvitationRecord
        {
            OrganizationId = organizationId,
            AdminEmail = adminEmail,
            Token = token,
            Status = (string)row.out_status,
            ExpiresAt = (DateTime)row.out_expires_at,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<List<PlatformOrganizationInvitationRecord>> GetLatestInvitationsAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<dynamic>(
            "SELECT * FROM fn_get_organization_latest_invitations()");

        var list = new List<PlatformOrganizationInvitationRecord>();
        foreach (var r in rows)
        {
            list.Add(new PlatformOrganizationInvitationRecord
            {
                OrganizationId = (Guid)r.out_organization_id,
                AdminEmail = (string)r.out_admin_email,
                Token = (string)r.out_token,
                Status = (string)r.out_status,
                ExpiresAt = (DateTime)r.out_expires_at,
                CreatedAt = (DateTime)r.out_created_at,
                AcceptedAt = r.out_accepted_at != null ? (DateTime)r.out_accepted_at : null
            });
        }
        return list;
    }

    public async Task<PlatformOrganizationInvitationValidationResult?> ValidateInvitationAsync(string token, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var r = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM fn_validate_organization_invitation(@p_token)",
            new { p_token = token });

        if (r == null) return null;
        return new PlatformOrganizationInvitationValidationResult
        {
            Valid = (bool)r.out_valid,
            InvitationId = (Guid?)r.out_invitation_id,
            OrganizationId = (Guid?)r.out_organization_id,
            TenantId = (Guid?)r.out_tenant_id,
            OrganizationName = (string)r.out_organization_name,
            AdminEmail = (string)r.out_admin_email,
            ExpiresAt = r.out_expires_at != null ? (DateTime)r.out_expires_at : null,
            Status = (string)r.out_status
        };
    }

    private class AcceptInvitationDbResult
    {
        public bool out_success { get; set; }
        public Guid? out_organization_id { get; set; }
        public Guid? out_tenant_id { get; set; }
        public string? out_role_name { get; set; }
    }

    public async Task<bool> AcceptInvitationAsync(string token, Guid userId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var r = await conn.QuerySingleOrDefaultAsync<AcceptInvitationDbResult>(
            "SELECT * FROM fn_accept_organization_invitation(@p_token, @p_user_id)",
            new { p_token = token, p_user_id = userId });

        return r?.out_success == true;
    }

    public async Task UpdateOrganizationAccountStatusAsync(Guid organizationId, string status, string? reason, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "public.sp_update_organization_account_status",
            new
            {
                p_org_id = organizationId,
                p_status = status,
                p_reason = reason
            },
            cancellationToken: ct);
    }

    public async Task UpdateOrganizationPlanQuotaAsync(
        Guid organizationId, 
        Guid planId, 
        string planCode, 
        string planName, 
        int maxBranches, 
        int maxClassrooms, 
        int maxChildren, 
        bool includesAi, 
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "public.sp_update_organization_plan_quota",
            new
            {
                p_org_id = organizationId,
                p_plan_id = planId,
                p_plan_code = planCode,
                p_plan_name = planName,
                p_max_branches = maxBranches,
                p_max_classrooms = maxClassrooms,
                p_max_children = maxChildren,
                p_includes_ai = includesAi
            },
            cancellationToken: ct);
    }

    public async Task<OrganizationQuotaUsage?> GetOrganizationQuotaUsageAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<OrganizationQuotaUsage>(
            "SELECT organization_id, plan_id, plan_code, plan_name, max_branches, current_branches, max_classrooms, current_classrooms, max_children, current_children, includes_ai FROM fn_get_organization_quota_usage(@p_org_id)",
            new { p_org_id = organizationId });
    }

    public async Task<List<PricingRegion>> GetPricingRegionsAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT id, code, name, description, currency, 
                   matched_cities::text AS MatchedCities, 
                   matched_states::text AS MatchedStates, 
                   is_default AS IsDefault, is_active AS IsActive, 
                   display_order AS DisplayOrder, created_at, updated_at
            FROM pricing_regions
            ORDER BY display_order ASC, name ASC";

        var rows = await conn.QueryAsync<PricingRegionDbRecord>(sql);
        var regions = rows.Select(r => r.ToDomain()).ToList();

        var tariffsSql = @"
            SELECT id, region_id, plan_id, plan_code, plan_name, name, frequency, 
                   price, discount_percentage, currency, is_active, created_at, updated_at
            FROM region_plan_tariffs
            ORDER BY plan_name ASC, price ASC";
        var allTariffs = (await conn.QueryAsync<RegionPlanTariffDbRecord>(tariffsSql))
            .Select(t => t.ToDomain())
            .ToList();

        foreach (var reg in regions)
        {
            reg.Tariffs = allTariffs.Where(t => t.RegionId == reg.Id).ToList();
        }

        return regions;
    }

    public async Task<PricingRegion?> ResolvePricingRegionByLocationAsync(string location, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT id, code, name, description, currency, 
                   matched_cities::text AS MatchedCities, 
                   matched_states::text AS MatchedStates, 
                   is_default AS IsDefault, is_active AS IsActive, 
                   0 AS DisplayOrder, NOW() AS created_at, NOW() AS updated_at
            FROM fn_resolve_pricing_region(@p_location)";

        var row = await conn.QuerySingleOrDefaultAsync<PricingRegionDbRecord>(sql, new { p_location = location });
        if (row == null) return null;

        var region = row.ToDomain();
        region.Tariffs = await GetRegionPlanTariffsAsync(region.Id, ct);
        return region;
    }

    public async Task<List<RegionPlanTariff>> GetRegionPlanTariffsAsync(Guid regionId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT id, region_id, plan_id, plan_code, plan_name, name, frequency, 
                   price, discount_percentage, currency, is_active, created_at, updated_at
            FROM region_plan_tariffs
            WHERE region_id = @regionId
            ORDER BY plan_name ASC, price ASC";

        var rows = await conn.QueryAsync<RegionPlanTariffDbRecord>(sql, new { regionId });
        return rows.Select(r => r.ToDomain()).ToList();
    }

    public async Task SavePricingRegionAsync(PricingRegion region, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            INSERT INTO pricing_regions (id, code, name, description, currency, matched_cities, matched_states, is_default, is_active, display_order, updated_at)
            VALUES (@Id, @Code, @Name, @Description, @Currency, @MatchedCities::jsonb, @MatchedStates::jsonb, @IsDefault, @IsActive, @DisplayOrder, NOW())
            ON CONFLICT (id) DO UPDATE SET
                code = EXCLUDED.code,
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                currency = EXCLUDED.currency,
                matched_cities = EXCLUDED.matched_cities,
                matched_states = EXCLUDED.matched_states,
                is_default = EXCLUDED.is_default,
                is_active = EXCLUDED.is_active,
                display_order = EXCLUDED.display_order,
                updated_at = NOW()";

        await conn.ExecuteAsync(sql, new
        {
            region.Id,
            region.Code,
            region.Name,
            region.Description,
            region.Currency,
            MatchedCities = System.Text.Json.JsonSerializer.Serialize(region.MatchedCities ?? new()),
            MatchedStates = System.Text.Json.JsonSerializer.Serialize(region.MatchedStates ?? new()),
            region.IsDefault,
            region.IsActive,
            region.DisplayOrder
        });
    }

    public async Task SaveRegionPlanTariffAsync(RegionPlanTariff tariff, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            INSERT INTO region_plan_tariffs (id, region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage, currency, is_active, updated_at)
            VALUES (@Id, @RegionId, @PlanId, @PlanCode, @PlanName, @Name, @Frequency, @Price, @DiscountPercentage, @Currency, @IsActive, NOW())
            ON CONFLICT (id) DO UPDATE SET
                region_id = EXCLUDED.region_id,
                plan_id = EXCLUDED.plan_id,
                plan_code = EXCLUDED.plan_code,
                plan_name = EXCLUDED.plan_name,
                name = EXCLUDED.name,
                frequency = EXCLUDED.frequency,
                price = EXCLUDED.price,
                discount_percentage = EXCLUDED.discount_percentage,
                currency = EXCLUDED.currency,
                is_active = EXCLUDED.is_active,
                updated_at = NOW()";

        await conn.ExecuteAsync(sql, new
        {
            tariff.Id,
            tariff.RegionId,
            tariff.PlanId,
            tariff.PlanCode,
            tariff.PlanName,
            tariff.Name,
            Frequency = tariff.Frequency.ToString(),
            tariff.Price,
            tariff.DiscountPercentage,
            tariff.Currency,
            tariff.IsActive
        });
    }

    public async Task AssignRegionalPlanToOrganizationAsync(Guid organizationId, Guid regionId, Guid planId, Guid? tariffId, string? currency = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            "CALL sp_assign_organization_regional_plan(@p_org_id, @p_region_id, @p_plan_id, @p_tariff_id, @p_currency)",
            new
            {
                p_org_id = organizationId,
                p_region_id = regionId,
                p_plan_id = planId,
                p_tariff_id = tariffId,
                p_currency = currency
            });
    }

    public async Task<List<OrganizationCurrencyHistory>> GetOrganizationCurrencyHistoryAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id, organization_id as OrganizationId, previous_currency as PreviousCurrency, 
                   new_currency as NewCurrency, changed_by as ChangedBy, reason, 
                   effective_from as EffectiveFrom, created_at as CreatedAt
            FROM public.organization_currency_history
            WHERE organization_id = @organizationId
            ORDER BY created_at DESC;";
        var results = await conn.QueryAsync<OrganizationCurrencyHistory>(sql, new { organizationId });
        return results.ToList();
    }

    private sealed class PricingRegionDbRecord
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Currency { get; set; } = "INR";
        public string? MatchedCities { get; set; }
        public string? MatchedStates { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public PricingRegion ToDomain() => new()
        {
            Id = Id,
            Code = Code,
            Name = Name,
            Description = Description,
            Currency = Currency,
            MatchedCities = string.IsNullOrEmpty(MatchedCities) ? new() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(MatchedCities) ?? new(),
            MatchedStates = string.IsNullOrEmpty(MatchedStates) ? new() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(MatchedStates) ?? new(),
            IsDefault = IsDefault,
            IsActive = IsActive,
            DisplayOrder = DisplayOrder,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    private sealed class RegionPlanTariffDbRecord
    {
        public Guid Id { get; set; }
        public Guid RegionId { get; set; }
        public Guid PlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Frequency { get; set; } = "Monthly";
        public decimal Price { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string Currency { get; set; } = "INR";
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public RegionPlanTariff ToDomain() => new()
        {
            Id = Id,
            RegionId = RegionId,
            PlanId = PlanId,
            PlanCode = PlanCode,
            PlanName = PlanName,
            Name = Name,
            Frequency = Enum.TryParse<BillingFrequency>(Frequency, true, out var f) ? f : BillingFrequency.Monthly,
            Price = Price,
            DiscountPercentage = DiscountPercentage,
            Currency = Currency,
            IsActive = IsActive,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}
