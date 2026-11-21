using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public sealed class PortalAuthRepository : IPortalAuthRepository
{
    private readonly IDbConnectionFactory _db;

    public PortalAuthRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    private sealed class AuthSyncRow
    {
        public string? out_status { get; set; }
        public Guid? out_user_id { get; set; }
        public Guid? out_tenant_id { get; set; }
        public Guid? out_effective_tenant_id { get; set; }
        public Guid? out_active_org_id { get; set; }
        public string? out_email { get; set; }
        public string? out_display_name { get; set; }
    }

    private sealed class OrgValidationRow
    {
        public bool is_valid { get; set; }
        public Guid? tenant_id { get; set; }
    }

    private sealed class AuthClaimDbRow
    {
        public string? claim_type { get; set; }
        public string? claim_value { get; set; }
    }

    private sealed class BranchRow
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
    }

    public async Task<PortalAuthSyncResult?> DevLoginAsync(string? email, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<AuthSyncRow>(
            "fn_dev_login",
            new { Email = email?.Trim() },
            cancellationToken: ct);
        return MapSync(row);
    }

    public async Task<PortalAuthSyncResult?> SyncExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string firstName,
        string lastName,
        string displayName,
        string portal,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<AuthSyncRow>(
            "fn_sync_external_login",
            new
            {
                Provider = provider,
                ProviderKey = providerKey,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = displayName,
                Portal = portal
            },
            cancellationToken: ct);
        return MapSync(row);
    }

    public async Task<IReadOnlyList<PortalAuthClaimRow>> GetUserAuthClaimsAsync(
        Guid userId,
        Guid? activeOrgId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<AuthClaimDbRow>(
            "fn_get_user_auth_claims",
            new { UserId = userId, ActiveOrgId = activeOrgId },
            cancellationToken: ct);

        return rows
            .Select(r => new PortalAuthClaimRow { ClaimType = r.claim_type, ClaimValue = r.claim_value })
            .ToList();
    }

    public async Task<PortalOrgValidationResult?> ValidateAndSetOrganizationAsync(
        Guid userId,
        Guid? organizationId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<OrgValidationRow>(
            "fn_validate_and_set_organization",
            new { UserId = userId, OrganizationId = organizationId },
            cancellationToken: ct);

        return row == null
            ? null
            : new PortalOrgValidationResult { IsValid = row.is_valid, TenantId = row.tenant_id };
    }

    public async Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT id FROM users WHERE normalized_email = UPPER(@Email) LIMIT 1",
            new { Email = email });
    }

    public async Task<PortalInvitationValidationResult?> ValidateOrganizationInvitationAsync(
        string token,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var val = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM fn_validate_organization_invitation(@p_token)",
            new { p_token = token.Trim() });

        if (val == null) return null;

        return new PortalInvitationValidationResult
        {
            Valid = (bool)val.out_valid,
            OrganizationId = (Guid?)val.out_organization_id,
            TenantId = (Guid?)val.out_tenant_id,
            OrganizationName = val.out_organization_name != null ? (string)val.out_organization_name : string.Empty,
            AdminEmail = val.out_admin_email != null ? (string)val.out_admin_email : string.Empty
        };
    }

    public async Task AcceptOrganizationInvitationAsync(
        string token,
        Guid userId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            "SELECT * FROM fn_accept_organization_invitation(@p_token, @p_user_id)",
            new { p_token = token.Trim(), p_user_id = userId });
    }

    public async Task<(Guid Id, string Name)?> GetActiveBranchInOrganizationAsync(
        Guid branchId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<BranchRow>(
            "SELECT id, name FROM branches WHERE id = @BranchId AND organization_id = @OrgId AND is_active = true",
            new { BranchId = branchId, OrgId = organizationId });
        return row == null ? null : (row.id, row.name);
    }

    public async Task SeedDevParentEnvironmentAsync(
        Guid userId,
        Guid tenantId,
        string email,
        Guid childId,
        string securityStamp,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_seed_dev_parent_environment",
            new
            {
                p_user_id = userId,
                p_tenant_id = tenantId,
                p_email = email,
                p_child_id = childId,
                p_security_stamp = securityStamp
            },
            cancellationToken: ct);
    }

    public async Task<(Guid? UserId, Guid TenantId, bool IsActive)> SyncParentExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string firstName,
        string lastName,
        string displayName,
        Guid defaultTenantId,
        Guid defaultOrgId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);

        var dynParams = new DynamicParameters();
        dynParams.Add("p_provider", provider);
        dynParams.Add("p_provider_key", providerKey);
        dynParams.Add("p_email", email);
        dynParams.Add("p_first_name", firstName);
        dynParams.Add("p_last_name", lastName);
        dynParams.Add("p_display_name", displayName);
        dynParams.Add("p_default_tenant_id", defaultTenantId);
        dynParams.Add("p_default_org_id", defaultOrgId);
        dynParams.Add("io_user_id", dbType: System.Data.DbType.Guid, direction: System.Data.ParameterDirection.InputOutput);
        dynParams.Add("io_tenant_id", dbType: System.Data.DbType.Guid, direction: System.Data.ParameterDirection.InputOutput);
        dynParams.Add("io_is_active", dbType: System.Data.DbType.Boolean, direction: System.Data.ParameterDirection.InputOutput);

        await conn.ExecuteAsync(
            "CALL sp_sync_parent_external_login(@p_provider, @p_provider_key, @p_email, @p_first_name, @p_last_name, @p_display_name, @p_default_tenant_id, @p_default_org_id, @io_user_id, @io_tenant_id, @io_is_active)",
            dynParams);

        var userId = dynParams.Get<Guid?>("io_user_id");
        var tenantId = dynParams.Get<Guid?>("io_tenant_id") ?? defaultTenantId;
        var isActive = dynParams.Get<bool?>("io_is_active") ?? false;

        return (userId, tenantId, isActive);
    }

    public async Task<bool> SyncParentRelationshipsAndCheckOnboardedAsync(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        Guid tenantId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);

        await conn.ExecuteProcedureAsync(
            "sp_sync_parent_relationships",
            new
            {
                p_user_id = userId,
                p_email = email,
                p_first_name = firstName,
                p_last_name = lastName,
                p_tenant_id = tenantId
            },
            cancellationToken: ct);

        return await conn.ExecuteScalarFunctionAsync<bool>(
            "public.fn_has_active_onboarded_child",
            new { p_user_id = userId },
            cancellationToken: ct);
    }

    private static PortalAuthSyncResult? MapSync(AuthSyncRow? row)
    {
        if (row == null) return null;
        return new PortalAuthSyncResult
        {
            OutStatus = row.out_status,
            OutUserId = row.out_user_id,
            OutTenantId = row.out_tenant_id,
            OutEffectiveTenantId = row.out_effective_tenant_id,
            OutActiveOrgId = row.out_active_org_id,
            OutEmail = row.out_email,
            OutDisplayName = row.out_display_name
        };
    }
}
