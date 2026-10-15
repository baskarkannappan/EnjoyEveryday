using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EmergencyContactRepository : IEmergencyContactRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmergencyContactRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<EmergencyContact>> GetContactsAsync(
        Guid tenantId,
        Guid? organizationId = null,
        Guid? branchId = null,
        bool activeOnly = false,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT * FROM emergency_contacts
            WHERE tenant_id = @TenantId
              AND (@ActiveOnly = false OR is_active = true)
              AND (
                  scope = 'Jurisdiction'
                  OR (scope = 'Organization' AND (@OrganizationId IS NULL OR organization_id IS NULL OR organization_id = @OrganizationId))
                  OR (scope = 'Branch' AND (@BranchId IS NULL OR branch_id IS NULL OR branch_id = @BranchId))
              )
            ORDER BY display_order ASC, name ASC";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryAsync<EmergencyContact>(sql, new
        {
            TenantId = tenantId,
            OrganizationId = organizationId,
            BranchId = branchId,
            ActiveOnly = activeOnly
        });
    }

    public async Task<EmergencyContact?> GetContactByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        const string sql = @"SELECT * FROM emergency_contacts WHERE tenant_id = @TenantId AND id = @Id";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<EmergencyContact>(sql, new { TenantId = tenantId, Id = id });
    }

    public async Task<EmergencyContact> CreateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO emergency_contacts (
                id, tenant_id, organization_id, branch_id, scope, category,
                name, phone_number, alternate_phone_number, country_region, address,
                instructions, verification_status, verified_by, verified_at,
                review_due_at, is_active, display_order, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @Scope, @Category,
                @Name, @PhoneNumber, @AlternatePhoneNumber, @CountryRegion, @Address,
                @Instructions, @VerificationStatus, @VerifiedBy, @VerifiedAt,
                @ReviewDueAt, @IsActive, @DisplayOrder, @CreatedAt, @UpdatedAt
            )";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, contact);
        return contact;
    }

    public async Task UpdateContactAsync(EmergencyContact contact, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE emergency_contacts SET
                organization_id = @OrganizationId,
                branch_id = @BranchId,
                scope = @Scope,
                category = @Category,
                name = @Name,
                phone_number = @PhoneNumber,
                alternate_phone_number = @AlternatePhoneNumber,
                country_region = @CountryRegion,
                address = @Address,
                instructions = @Instructions,
                verification_status = @VerificationStatus,
                verified_by = @VerifiedBy,
                verified_at = @VerifiedAt,
                review_due_at = @ReviewDueAt,
                is_active = @IsActive,
                display_order = @DisplayOrder,
                updated_at = CURRENT_TIMESTAMP
            WHERE tenant_id = @TenantId AND id = @Id";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, contact);
    }

    public async Task DeleteContactAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        const string sql = @"DELETE FROM emergency_contacts WHERE tenant_id = @TenantId AND id = @Id";
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { TenantId = tenantId, Id = id });
    }

    public async Task VerifyContactAsync(
        Guid tenantId,
        Guid id,
        string verifiedBy,
        DateTime verifiedAt,
        DateTime? reviewDueAt,
        CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE emergency_contacts SET
                verification_status = 'Verified',
                verified_by = @VerifiedBy,
                verified_at = @VerifiedAt,
                review_due_at = @ReviewDueAt,
                updated_at = CURRENT_TIMESTAMP
            WHERE tenant_id = @TenantId AND id = @Id";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Id = id,
            VerifiedBy = verifiedBy,
            VerifiedAt = verifiedAt,
            ReviewDueAt = reviewDueAt
        });
    }
}
