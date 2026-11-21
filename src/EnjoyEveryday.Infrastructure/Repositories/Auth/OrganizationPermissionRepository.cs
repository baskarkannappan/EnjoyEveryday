using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public sealed class OrganizationPermissionRepository : IOrganizationPermissionRepository
{
    private readonly IDbConnectionFactory _db;

    public OrganizationPermissionRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    private sealed class PermissionRecord
    {
        public string role_name { get; set; } = string.Empty;
        public string feature_key { get; set; } = string.Empty;
        public bool is_allowed { get; set; }
    }

    public async Task<IReadOnlyList<OrganizationRolePermissionOverride>> GetOverridesAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var records = await conn.QueryFunctionAsync<PermissionRecord>(
            "fn_get_organization_role_permissions",
            new { p_tenant_id = tenantId, p_organization_id = organizationId },
            cancellationToken: ct);

        return records
            .Select(r => new OrganizationRolePermissionOverride(r.role_name, r.feature_key, r.is_allowed))
            .ToList();
    }

    public async Task UpsertOverrideAsync(
        Guid tenantId,
        Guid organizationId,
        string roleName,
        string featureKey,
        bool isAllowed,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_upsert_organization_role_permission",
            new
            {
                p_tenant_id = tenantId,
                p_organization_id = organizationId,
                p_role_name = roleName,
                p_feature_key = featureKey,
                p_is_allowed = isAllowed
            },
            cancellationToken: ct);
    }

    public async Task ResetOverridesAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_reset_organization_role_permissions",
            new
            {
                p_tenant_id = tenantId,
                p_organization_id = organizationId
            },
            cancellationToken: ct);
    }
}
