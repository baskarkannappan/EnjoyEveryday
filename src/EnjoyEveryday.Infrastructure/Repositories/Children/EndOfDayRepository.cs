using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EndOfDayRepository : IEndOfDayRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EndOfDayRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingTeachersAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT ta.id AS Id,
                   COALESCE(t.display_name, u.display_name, u.first_name || ' ' || u.last_name, 'Educator') AS Name
            FROM teacher_attendance ta
            LEFT JOIN teachers t ON ta.teacher_id = t.id
            LEFT JOIN users u ON t.user_id = u.id OR ta.teacher_id = u.id
            WHERE ta.tenant_id = @TenantId AND ta.attendance_date = @Date
              AND ta.status IN ('Present', 'Late') AND ta.clock_out_time IS NULL
              AND (@BranchId::uuid IS NULL OR ta.branch_id = @BranchId)";
        return (await c.QueryAsync<EndOfDayPendingItem>(sql, new { TenantId = tenantId, Date = date, BranchId = branchId })).ToList();
    }

    public async Task CloseTeachersAsync(Guid[] ids, TimeSpan clockOut, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteAsync(@"
            UPDATE teacher_attendance
            SET clock_out_time = @ClockOut,
                notes = CASE WHEN notes IS NULL OR notes = '' THEN 'System auto-checkout at End of Day'
                             ELSE notes || ' · System auto-checkout at End of Day' END,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = ANY(@Ids)", new { ClockOut = clockOut, Ids = ids });
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingChildrenAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT ca.id AS Id, c.first_name || ' ' || c.last_name AS Name
            FROM child_attendance ca
            JOIN children c ON ca.child_id = c.id
            WHERE ca.tenant_id = @TenantId AND ca.attendance_date = @Date
              AND ca.status IN ('Present', 'Late') AND ca.departure_time IS NULL
              AND (@BranchId::uuid IS NULL OR ca.branch_id = @BranchId)";
        return (await c.QueryAsync<EndOfDayPendingItem>(sql, new { TenantId = tenantId, Date = date, BranchId = branchId })).ToList();
    }

    public async Task CloseChildrenAsync(Guid[] ids, TimeSpan departure, Guid userId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteAsync(@"
            UPDATE child_attendance
            SET departure_time = @Departure,
                pickup_person_name = 'System Auto-Closed',
                pickup_person_relationship = 'System Process',
                pickup_verification_method = 'System Auto-Closed',
                pickup_notes = CASE WHEN pickup_notes IS NULL OR pickup_notes = '' THEN 'System auto-checkout at End of Day'
                                    ELSE pickup_notes || ' · System auto-checkout at End of Day' END,
                departure_verified_by_user_id = @UserId,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = ANY(@Ids)", new { Departure = departure, UserId = userId, Ids = ids });
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingActivitiesAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT es.id AS Id, COALESCE(e.title, 'Planned Experience') AS Name
            FROM experience_schedules es
            LEFT JOIN experiences e ON es.experience_id = e.id
            LEFT JOIN classrooms cr ON es.classroom_id = cr.id
            WHERE es.tenant_id = @TenantId AND es.scheduled_date = @Date
              AND es.status IN ('Planned', 'Scheduled', 'Confirmed', 'Started', 'Active', 'InProgress')
              AND (@BranchId::uuid IS NULL OR cr.branch_id = @BranchId)";
        return (await c.QueryAsync<EndOfDayPendingItem>(sql, new { TenantId = tenantId, Date = DateOnly.FromDateTime(date), BranchId = branchId })).ToList();
    }

    public async Task CloseActivitiesAsync(Guid tenantId, DateTime date, Guid[] scheduleIds, TimeSpan endTime, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteAsync(@"
            UPDATE experience_schedules
            SET status = 'Completed',
                preparation_notes = CASE WHEN preparation_notes IS NULL OR preparation_notes = '' THEN 'System Auto-Closed at End of Day'
                                         WHEN preparation_notes LIKE '%System Auto-Closed%' THEN preparation_notes
                                         ELSE preparation_notes || ' [System Auto-Closed]' END,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = ANY(@Ids)", new { Ids = scheduleIds });

        await c.ExecuteAsync(@"
            UPDATE experience_sessions
            SET status = 'Completed', end_time = @EndTime,
                actual_notes = CASE WHEN actual_notes IS NULL OR actual_notes = '' THEN 'System Auto-Closed at End of Day'
                                    WHEN actual_notes LIKE '%System Auto-Closed%' THEN actual_notes
                                    ELSE actual_notes || ' [System Auto-Closed]' END,
                updated_at = CURRENT_TIMESTAMP
            WHERE tenant_id = @TenantId
              AND experience_schedule_id = ANY(@ScheduleIds)
              AND status NOT IN ('Completed', 'Cancelled', 'Finished')",
            new { EndTime = endTime, TenantId = tenantId, ScheduleIds = scheduleIds });
    }

    public async Task<List<EndOfDayPendingItem>> GetPendingEventsAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT ev.id AS Id, ev.title AS Name
            FROM events ev
            WHERE ev.tenant_id = @TenantId AND ev.event_date = @Date
              AND ev.status IN ('Draft', 'Planned', 'Approved', 'Published', 'InProgress')
              AND (@BranchId::uuid IS NULL OR ev.scope = 'Organization' OR ev.is_organization_wide = true OR ev.branch_id = @BranchId)";
        return (await c.QueryAsync<EndOfDayPendingItem>(sql, new { TenantId = tenantId, Date = DateOnly.FromDateTime(date), BranchId = branchId })).ToList();
    }

    public async Task CloseEventsAsync(Guid[] ids, CancellationToken ct = default)
    {
        using var c = await _connectionFactory.CreateConnectionAsync(ct);
        await c.ExecuteAsync(@"
            UPDATE events
            SET status = 'Completed',
                description = CASE WHEN description IS NULL OR description = '' THEN 'System Auto-Closed at End of Day'
                                   WHEN description LIKE '%System Auto-Closed%' THEN description
                                   ELSE description || ' [System Auto-Closed]' END,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = ANY(@Ids)", new { Ids = ids });
    }
}
