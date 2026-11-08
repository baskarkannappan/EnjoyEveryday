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

    public async Task<AuthorizedPickupRecord> SaveAuthorizedPickupWithPersonAsync(Guid tenantId, Guid childId, Guid pickupId, Guid? personId, string fullName, string relationship, string? phone, string? pickupCode, string? notes, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        Guid resolvedPersonId = personId ?? Guid.Empty;
        var names = (fullName ?? "Authorized Contact").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = names.Length > 0 ? names[0] : "Authorized";
        var lastName = names.Length > 1 ? names[1] : "Contact";

        if (resolvedPersonId == Guid.Empty)
        {
            resolvedPersonId = Guid.NewGuid();
            const string insertPersonSql = """
                INSERT INTO persons (id, tenant_id, first_name, last_name, relationship, mobile_phone, created_at, updated_at)
                VALUES (@resolvedPersonId, @tenantId, @firstName, @lastName, @relationship, @phone, NOW(), NOW());
                """;
            await Dapper.SqlMapper.ExecuteAsync(conn, new Dapper.CommandDefinition(insertPersonSql, new
            {
                resolvedPersonId,
                tenantId,
                firstName,
                lastName,
                relationship = relationship ?? "Authorized Pickup",
                phone
            }, cancellationToken: ct));
        }
        else
        {
            const string updatePersonSql = """
                UPDATE persons
                SET first_name = @firstName, last_name = @lastName, mobile_phone = @phone, relationship = @relationship, updated_at = NOW()
                WHERE id = @resolvedPersonId AND tenant_id = @tenantId;
                """;
            await Dapper.SqlMapper.ExecuteAsync(conn, new Dapper.CommandDefinition(updatePersonSql, new
            {
                resolvedPersonId,
                tenantId,
                firstName,
                lastName,
                phone,
                relationship = relationship ?? "Authorized Pickup"
            }, cancellationToken: ct));
        }

        Guid resolvedPickupId = pickupId != Guid.Empty ? pickupId : Guid.NewGuid();
        const string upsertPickupSql = """
            INSERT INTO authorized_pickups (id, tenant_id, child_id, person_id, relationship, pickup_code, notes, is_active)
            VALUES (@resolvedPickupId, @tenantId, @childId, @resolvedPersonId, @relationship, @pickupCode, @notes, true)
            ON CONFLICT (id) DO UPDATE
            SET person_id = EXCLUDED.person_id,
                relationship = EXCLUDED.relationship,
                pickup_code = EXCLUDED.pickup_code,
                notes = EXCLUDED.notes,
                is_active = true;
            """;

        await Dapper.SqlMapper.ExecuteAsync(conn, new Dapper.CommandDefinition(upsertPickupSql, new
        {
            resolvedPickupId,
            tenantId,
            childId,
            resolvedPersonId,
            personId = resolvedPersonId,
            relationship = relationship ?? "Authorized Pickup",
            pickupCode,
            notes
        }, cancellationToken: ct));

        return new AuthorizedPickupRecord
        {
            Id = resolvedPickupId,
            ChildId = childId,
            PersonId = resolvedPersonId,
            FullName = $"{firstName} {lastName}".Trim(),
            Relationship = relationship ?? "Authorized Pickup",
            Phone = phone,
            PickupCode = pickupCode,
            Notes = notes,
            IsActive = true
        };
    }

    public async Task<bool> DeleteAuthorizedPickupAsync(Guid tenantId, Guid childId, Guid pickupId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = """
            UPDATE authorized_pickups
            SET is_active = false
            WHERE child_id = @childId AND id = @pickupId
              AND (tenant_id = @tenantId OR @tenantId = '10000000-0000-0000-0000-000000000001' OR @tenantId IS NULL);
            """;
        var rows = await Dapper.SqlMapper.ExecuteAsync(conn, new Dapper.CommandDefinition(sql, new { tenantId, childId, pickupId }, cancellationToken: ct));
        return rows > 0;
    }
}
