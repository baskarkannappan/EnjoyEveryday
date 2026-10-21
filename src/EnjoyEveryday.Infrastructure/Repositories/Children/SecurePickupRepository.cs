using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class SecurePickupRepository : ISecurePickupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SecurePickupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ChildPickupHeaderData?> GetChildPickupHeaderAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var childSql = @"
            SELECT c.id, c.first_name AS FirstName, c.last_name AS LastName, c.photo_url AS PhotoUrl,
                   cr.name AS ClassroomName, b.name AS BranchName, cr.id AS ClassroomId, b.id AS BranchId
            FROM children c
            LEFT JOIN classrooms cr ON c.classroom_id = cr.id
            LEFT JOIN branches b ON cr.branch_id = b.id
            WHERE c.tenant_id = @TenantId AND c.id = @ChildId";

        return await conn.QueryFirstOrDefaultAsync<ChildPickupHeaderData>(childSql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task<IReadOnlyList<CustodyRestrictionRecord>> GetActiveCustodyRestrictionsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var restrictionsSql = @"
            SELECT r.id, r.restriction_type AS RestrictionType, r.notes, r.effective_from AS EffectiveFrom, r.effective_to AS EffectiveTo,
                   COALESCE(p.first_name || ' ' || p.last_name, 'Restricted Individual') AS PersonName
            FROM child_person_restrictions r
            LEFT JOIN persons p ON r.person_id = p.id
            WHERE r.tenant_id = @TenantId AND r.child_id = @ChildId
              AND (r.effective_to IS NULL OR r.effective_to >= NOW())";

        var rows = await conn.QueryAsync<CustodyRestrictionRecord>(restrictionsSql, new { TenantId = tenantId, ChildId = childId });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AuthorizedPickupRecord>> GetAuthorizedPickupsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var pickupsSql = @"
            SELECT ap.id, ap.child_id AS ChildId, ap.person_id AS PersonId, ap.relationship, ap.pickup_code AS PickupCode,
                   ap.photo_url AS PhotoUrl, ap.is_active AS IsActive, ap.notes,
                   COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), ap.relationship, 'Authorized Pickup') AS FullName,
                   COALESCE(p.mobile_phone, p.alternate_phone) AS Phone
            FROM authorized_pickups ap
            LEFT JOIN persons p ON ap.person_id = p.id
            WHERE ap.tenant_id = @TenantId AND ap.child_id = @ChildId AND ap.is_active = TRUE
            ORDER BY ap.is_active DESC";

        var rows = await conn.QueryAsync<AuthorizedPickupRecord>(pickupsSql, new { TenantId = tenantId, ChildId = childId });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<FamilyPickupRelationshipRecord>> GetFamilyPickupRelationshipsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var familySql = @"
            WITH guardians AS (
                SELECT 
                    fr.id AS Id,
                    fr.child_id AS ChildId,
                    COALESCE(p.id, u.id) AS PersonId,
                    COALESCE(fr.relationship, p.relationship, 'Legal Guardian') AS Relationship,
                    fr.is_primary AS IsPrimary,
                    FALSE AS IsEmergency,
                    COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), NULLIF(TRIM(u.first_name || ' ' || u.last_name), ''), u.display_name, 'Parent / Guardian') AS FullName,
                    COALESCE(p.mobile_phone, p.alternate_phone) AS Phone,
                    NULL::text AS PhotoUrl
                FROM family_relationships fr
                JOIN users u ON fr.user_id = u.id
                LEFT JOIN persons p ON p.user_id = u.id
                WHERE fr.tenant_id = @TenantId AND fr.child_id = @ChildId
                  AND (fr.status IS NULL OR fr.status = 'Verified')
                  AND fr.revoked_at IS NULL

                UNION ALL

                SELECT 
                    cg.id AS Id,
                    cg.child_id AS ChildId,
                    p.id AS PersonId,
                    COALESCE(cg.relationship, p.relationship, 'Legal Guardian') AS Relationship,
                    cg.is_primary AS IsPrimary,
                    FALSE AS IsEmergency,
                    COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), 'Parent / Guardian') AS FullName,
                    COALESCE(p.mobile_phone, p.alternate_phone) AS Phone,
                    NULL::text AS PhotoUrl
                FROM child_guardians cg
                JOIN persons p ON cg.person_id = p.id
                WHERE cg.tenant_id = @TenantId AND cg.child_id = @ChildId
                  AND (cg.can_pickup = TRUE OR cg.is_primary = TRUE)
            )
            SELECT DISTINCT ON (PersonId)
                   Id, ChildId, PersonId, Relationship, IsPrimary, IsEmergency, FullName, Phone, PhotoUrl
            FROM guardians
            ORDER BY PersonId, IsPrimary DESC";

        var rows = await conn.QueryAsync<FamilyPickupRelationshipRecord>(familySql, new { TenantId = tenantId, ChildId = childId });
        return rows.ToList();
    }

    public async Task<ChildAttendance?> GetTodayAttendanceAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var attSql = @"
            SELECT * FROM child_attendance
            WHERE tenant_id = @TenantId AND child_id = @ChildId AND attendance_date = @Date
            LIMIT 1";

        return await conn.QueryFirstOrDefaultAsync<ChildAttendance>(attSql, new { TenantId = tenantId, ChildId = childId, Date = date.Date });
    }

    public async Task AddAuthorizedPickupAsync(AuthorizedPickup pickup, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            INSERT INTO authorized_pickups (
                id, tenant_id, child_id, person_id, relationship, pickup_code, photo_url, valid_from, valid_to, is_active, notes
            )
            VALUES (
                @Id, @TenantId, @ChildId, @PersonId, @Relationship, @PickupCode, @PhotoUrl, @ValidFrom, @ValidTo, @IsActive, @Notes
            )";

        await conn.ExecuteAsync(sql, pickup);
    }

    public async Task<bool> AuthorizedPickupExistsAsync(Guid tenantId, Guid pickupId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = "SELECT 1 FROM authorized_pickups WHERE tenant_id = @TenantId AND id = @PickupId LIMIT 1";
        var res = await conn.ExecuteScalarAsync<int?>(sql, new { TenantId = tenantId, PickupId = pickupId });
        return res.HasValue && res.Value == 1;
    }
}
