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

        var dynamicParams = new Dapper.DynamicParameters();
        dynamicParams.Add("p_tenant_id", tenantId);
        dynamicParams.Add("p_child_id", childId);
        dynamicParams.Add("p_first_name", firstName);
        dynamicParams.Add("p_last_name", lastName);
        dynamicParams.Add("p_relationship", relationship ?? "Authorized Pickup");
        dynamicParams.Add("p_phone", phone);
        dynamicParams.Add("p_pickup_code", pickupCode);
        dynamicParams.Add("p_notes", notes);
        dynamicParams.Add("p_pickup_id", pickupId != Guid.Empty ? pickupId : Guid.NewGuid(), System.Data.DbType.Guid, System.Data.ParameterDirection.InputOutput);
        dynamicParams.Add("p_person_id", resolvedPersonId != Guid.Empty ? resolvedPersonId : (Guid?)null, System.Data.DbType.Guid, System.Data.ParameterDirection.InputOutput);

        await conn.ExecuteAsync(new Dapper.CommandDefinition("CALL sp_save_authorized_pickup_with_person(@p_tenant_id, @p_child_id, @p_first_name, @p_last_name, @p_relationship, @p_phone, @p_pickup_code, @p_notes, @p_pickup_id, @p_person_id)", dynamicParams, cancellationToken: ct));

        var returnedPickupId = dynamicParams.Get<Guid>("p_pickup_id");
        var returnedPersonId = dynamicParams.Get<Guid>("p_person_id");

        return new AuthorizedPickupRecord
        {
            Id = returnedPickupId,
            ChildId = childId,
            PersonId = returnedPersonId,
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
        await conn.ExecuteProcedureAsync(
            "sp_delete_authorized_pickup",
            new { p_tenant_id = tenantId, p_child_id = childId, p_pickup_id = pickupId },
            cancellationToken: ct);
        return true;
    }
}
