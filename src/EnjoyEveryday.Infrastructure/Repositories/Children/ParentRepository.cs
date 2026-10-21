using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ParentRepository : IParentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ParentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                c.id AS ChildId,
                c.tenant_id AS TenantId,
                c.first_name AS FirstName,
                c.last_name AS LastName,
                c.date_of_birth AS DateOfBirth,
                c.classroom_id AS ClassroomId,
                cl.name AS ClassroomName,
                cl.branch_id AS BranchId,
                b.name AS BranchName,
                COALESCE(b.organization_id, o.id, (SELECT o2.id FROM organizations o2 WHERE o2.tenant_id = c.tenant_id ORDER BY o2.created_at LIMIT 1)) AS OrganizationId,
                o.name AS OrganizationName,
                c.photo_url AS PhotoUrl,
                fr.relationship AS Relationship,
                fr.is_primary AS IsPrimary
            FROM family_relationships fr
            JOIN children c ON fr.child_id = c.id
            LEFT JOIN classrooms cl ON c.classroom_id = cl.id
            LEFT JOIN branches b ON cl.branch_id = b.id
            LEFT JOIN organizations o ON b.organization_id = o.id
            WHERE fr.user_id = @UserId 
              AND fr.status = 'Verified' AND fr.revoked_at IS NULL
              AND c.is_active = true
            ORDER BY c.first_name, c.last_name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<ParentAuthorizedChild>(sql, new { UserId = userId });
    }

    public async Task<bool> HasChildAccessAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 
                FROM family_relationships fr
                JOIN children c ON fr.child_id = c.id
                WHERE fr.user_id = @UserId 
                  AND fr.child_id = @ChildId
                  AND fr.status = 'Verified' AND fr.revoked_at IS NULL
                  AND c.is_active = true
            )";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = userId, ChildId = childId });
    }

    public async Task<ChildAttendance?> GetChildAttendanceForDateAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tenant_id, branch_id, classroom_id, child_id, attendance_date, status, arrival_time, departure_time, notes, recorded_by_user_id, created_at, updated_at
            FROM child_attendance
            WHERE child_id = @ChildId AND attendance_date = @Date
            LIMIT 1";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<ChildAttendance>(sql, new { ChildId = childId, Date = date.Date });
    }

    public async Task<IEnumerable<ChildJourneyEntry>> GetApprovedJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT *
            FROM child_journey_entries
            WHERE child_id = @ChildId 
              AND is_parent_visible = true
            ORDER BY observation_date DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<ChildJourneyEntry>(sql, new { ChildId = childId });
    }

    public async Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT *
            FROM child_milestones
            WHERE child_id = @ChildId 
              AND parent_visible = true
            ORDER BY milestone_date DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<ChildMilestone>(sql, new { ChildId = childId });
    }

    public async Task<IEnumerable<Event>> GetApprovedEventsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DISTINCT e.*
            FROM events e
            JOIN children c ON c.id = @ChildId
            LEFT JOIN event_classrooms ec ON e.id = ec.event_id
            WHERE (e.tenant_id = c.tenant_id OR e.tenant_id = @TenantId)
              AND e.status IN ('Published', 'InProgress', 'Completed')
              AND (
                  e.is_organization_wide = true
                  OR ec.classroom_id = c.classroom_id
                  OR e.branch_id IN (SELECT cl.branch_id FROM classrooms cl WHERE cl.id = c.classroom_id)
              )
            ORDER BY e.event_date DESC, e.start_time";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Event>(sql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task<Person?> GetPersonByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT *
            FROM persons
            WHERE user_id = @UserId
            ORDER BY (tenant_id = @TenantId) DESC
            LIMIT 1";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Person>(sql, new { TenantId = tenantId, UserId = userId });
    }

    public async Task UpdatePersonContactAsync(Guid tenantId, Guid personId, string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE persons
            SET mobile_phone = @MobilePhone,
                preferred_contact_method = @PreferredContactMethod,
                preferred_language = @PreferredLanguage,
                updated_at = NOW()
            WHERE id = @PersonId AND tenant_id = @TenantId";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new {
            TenantId = tenantId,
            PersonId = personId,
            MobilePhone = mobilePhone,
            PreferredContactMethod = preferredContactMethod,
            PreferredLanguage = preferredLanguage
        });
    }
}
