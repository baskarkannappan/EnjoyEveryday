using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EmergencyContactRepository : IEmergencyContactRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmergencyContactRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] CreateContactParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "Scope", "Category",
        "Name", "PhoneNumber", "AlternatePhoneNumber", "CountryRegion", "Address",
        "Instructions", "VerificationStatus", "VerifiedBy", "VerifiedAt",
        "ReviewDueAt", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateContactParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "Scope", "Category",
        "Name", "PhoneNumber", "AlternatePhoneNumber", "CountryRegion", "Address",
        "Instructions", "VerificationStatus", "VerifiedBy", "VerifiedAt",
        "ReviewDueAt", "IsActive", "DisplayOrder"
    ];

    public async Task<IEnumerable<EmergencyContact>> GetContactsAsync(
        Guid tenantId,
        Guid? organizationId = null,
        Guid? branchId = null,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFunctionAsync<EmergencyContact>(
            "fn_get_emergency_contacts",
            new
            {
                TenantId = tenantId,
                OrganizationId = organizationId,
                BranchId = branchId,
                ActiveOnly = activeOnly
            },
            cancellationToken: ct);
    }

    public async Task<EmergencyContact?> GetContactByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<EmergencyContact>(
            "fn_get_emergency_contact_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: ct);
    }

    public async Task<EmergencyContact> CreateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        if (contact.Id == Guid.Empty) contact.Id = Guid.NewGuid();

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_emergency_contact",
            contact,
            parameterNames: CreateContactParams,
            cancellationToken: ct);

        return contact;
    }

    public async Task UpdateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_emergency_contact",
            contact,
            parameterNames: UpdateContactParams,
            cancellationToken: ct);
    }

    public async Task DeleteContactAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_delete_emergency_contact",
            new { TenantId = tenantId, Id = id },
            cancellationToken: ct);
    }

    public async Task VerifyContactAsync(
        Guid tenantId,
        Guid id,
        string verifiedBy,
        DateTime verifiedAt,
        DateTime? reviewDueAt,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_verify_emergency_contact",
            new
            {
                TenantId = tenantId,
                Id = id,
                VerifiedBy = verifiedBy,
                VerifiedAt = verifiedAt,
                ReviewDueAt = reviewDueAt
            },
            cancellationToken: ct);
    }
}
