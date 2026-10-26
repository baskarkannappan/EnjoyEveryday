using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class SecurePickupRepository : ISecurePickupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SecurePickupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] AddPickupParams =
    [
        "Id", "TenantId", "ChildId", "PersonId", "Relationship", "PickupCode",
        "PhotoUrl", "ValidFrom", "ValidTo", "IsActive", "Notes"
    ];

    public async Task<ChildPickupHeaderData?> GetChildPickupHeaderAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<ChildPickupHeaderData>(
            "fn_get_child_pickup_header",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);
    }

    public async Task<IReadOnlyList<CustodyRestrictionRecord>> GetActiveCustodyRestrictionsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<CustodyRestrictionRecord>(
            "fn_get_active_custody_restrictions",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AuthorizedPickupRecord>> GetAuthorizedPickupsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<AuthorizedPickupRecord>(
            "fn_get_authorized_pickups",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<FamilyPickupRelationshipRecord>> GetFamilyPickupRelationshipsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<FamilyPickupRelationshipRecord>(
            "fn_get_family_pickup_relationships",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<ChildAttendance?> GetTodayAttendanceAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<ChildAttendance>(
            "fn_get_today_child_attendance",
            new { TenantId = tenantId, ChildId = childId, Date = date.Date },
            cancellationToken: ct);
    }

    public async Task AddAuthorizedPickupAsync(AuthorizedPickup pickup, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_add_authorized_pickup",
            pickup,
            parameterNames: AddPickupParams,
            cancellationToken: ct);
    }

    public async Task<bool> AuthorizedPickupExistsAsync(Guid tenantId, Guid pickupId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarFunctionAsync<bool>(
            "fn_authorized_pickup_exists",
            new { TenantId = tenantId, PickupId = pickupId },
            cancellationToken: ct);
    }
}
