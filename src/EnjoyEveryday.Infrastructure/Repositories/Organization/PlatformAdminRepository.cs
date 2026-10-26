using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
}
