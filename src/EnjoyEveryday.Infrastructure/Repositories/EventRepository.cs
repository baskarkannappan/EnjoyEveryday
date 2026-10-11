using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EventRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Event?> GetByIdAsync(Guid tenantId, Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Event>(
            "SELECT * FROM events WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId }
        );
    }

    public async Task<IEnumerable<Event>> GetAllForTenantAsync(Guid tenantId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<Event>(
            "SELECT * FROM events WHERE tenant_id = @TenantId ORDER BY event_date DESC",
            new { TenantId = tenantId }
        );
    }

    public async Task<IEnumerable<Event>> GetByClassroomAsync(Guid tenantId, Guid classroomId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            SELECT e.* FROM events e
            LEFT JOIN event_classrooms ec ON e.id = ec.event_id
            WHERE e.tenant_id = @TenantId 
              AND (e.is_organization_wide = true OR ec.classroom_id = @ClassroomId)
            ORDER BY e.event_date DESC";
        return await connection.QueryAsync<Event>(sql, new { TenantId = tenantId, ClassroomId = classroomId });
    }

    public async Task<Guid> CreateAsync(Event evt)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            INSERT INTO events (id, tenant_id, title, description, event_type_id, event_date, start_time, end_time, location, organizer_id, status, is_organization_wide, created_at, updated_at)
            VALUES (@Id, @TenantId, @Title, @Description, @EventTypeId, @EventDate, @StartTime, @EndTime, @Location, @OrganizerId, @Status, @IsOrganizationWide, @CreatedAt, @UpdatedAt)
            RETURNING id";
        return await connection.ExecuteScalarAsync<Guid>(sql, evt);
    }

    public async Task UpdateAsync(Event evt)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        evt.UpdatedAt = DateTimeOffset.UtcNow;
        var sql = @"
            UPDATE events 
            SET title = @Title, description = @Description, event_type_id = @EventTypeId, 
                event_date = @EventDate, start_time = @StartTime, end_time = @EndTime, 
                location = @Location, organizer_id = @OrganizerId, status = @Status, 
                is_organization_wide = @IsOrganizationWide, updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";
        await connection.ExecuteAsync(sql, evt);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM events WHERE id = @Id AND tenant_id = @TenantId", new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<EventType>> GetEventTypesAsync(Guid tenantId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<EventType>(
            "SELECT * FROM event_types WHERE tenant_id = @TenantId AND is_active = true ORDER BY name",
            new { TenantId = tenantId }
        );
    }

    public async Task<Guid> CreateEventTypeAsync(EventType eventType)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            INSERT INTO event_types (id, tenant_id, name, description, is_active, created_at, updated_at)
            VALUES (@Id, @TenantId, @Name, @Description, @IsActive, @CreatedAt, @UpdatedAt)
            RETURNING id";
        return await connection.ExecuteScalarAsync<Guid>(sql, eventType);
    }

    public async Task AddEventClassroomAsync(EventClassroom eventClassroom)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            INSERT INTO event_classrooms (event_id, classroom_id) 
            VALUES (@EventId, @ClassroomId)
            ON CONFLICT DO NOTHING";
        await connection.ExecuteAsync(sql, eventClassroom);
    }

    public async Task RemoveEventClassroomAsync(Guid eventId, Guid classroomId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM event_classrooms WHERE event_id = @EventId AND classroom_id = @ClassroomId", new { EventId = eventId, ClassroomId = classroomId });
    }

    public async Task<IEnumerable<Guid>> GetEventClassroomsAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<Guid>("SELECT classroom_id FROM event_classrooms WHERE event_id = @EventId", new { EventId = eventId });
    }

    public async Task AddEventExperienceAsync(EventExperience eventExperience)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            INSERT INTO event_experiences (id, event_id, experience_id, experience_schedule_id, experience_session_id)
            VALUES (@Id, @EventId, @ExperienceId, @ExperienceScheduleId, @ExperienceSessionId)";
        await connection.ExecuteAsync(sql, eventExperience);
    }

    public async Task RemoveEventExperienceAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM event_experiences WHERE id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<EventExperience>> GetEventExperiencesAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<EventExperience>("SELECT * FROM event_experiences WHERE event_id = @EventId", new { EventId = eventId });
    }

    public async Task<Guid> CreateEventTaskAsync(EventTask task)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            INSERT INTO event_tasks (id, event_id, description, owner_id, due_date, is_completed, completed_at, created_at, updated_at)
            VALUES (@Id, @EventId, @Description, @OwnerId, @DueDate, @IsCompleted, @CompletedAt, @CreatedAt, @UpdatedAt)
            RETURNING id";
        return await connection.ExecuteScalarAsync<Guid>(sql, task);
    }

    public async Task UpdateEventTaskAsync(EventTask task)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        task.UpdatedAt = DateTimeOffset.UtcNow;
        var sql = @"
            UPDATE event_tasks 
            SET description = @Description, owner_id = @OwnerId, due_date = @DueDate, 
                is_completed = @IsCompleted, completed_at = @CompletedAt, updated_at = @UpdatedAt
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, task);
    }

    public async Task DeleteEventTaskAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM event_tasks WHERE id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<EventTask>> GetEventTasksAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<EventTask>("SELECT * FROM event_tasks WHERE event_id = @EventId ORDER BY created_at", new { EventId = eventId });
    }

    public async Task<Guid> CreateEventRsvpAsync(EventRsvp rsvp)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            INSERT INTO event_rsvps (id, event_id, child_id, guardian_id, status, notes, created_at, updated_at)
            VALUES (@Id, @EventId, @ChildId, @GuardianId, @Status, @Notes, @CreatedAt, @UpdatedAt)
            RETURNING id";
        return await connection.ExecuteScalarAsync<Guid>(sql, rsvp);
    }

    public async Task UpdateEventRsvpAsync(EventRsvp rsvp)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        rsvp.UpdatedAt = DateTimeOffset.UtcNow;
        var sql = @"
            UPDATE event_rsvps 
            SET status = @Status, notes = @Notes, updated_at = @UpdatedAt
            WHERE id = @Id";
        await connection.ExecuteAsync(sql, rsvp);
    }

    public async Task<IEnumerable<EventRsvp>> GetEventRsvpsAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<EventRsvp>("SELECT * FROM event_rsvps WHERE event_id = @EventId", new { EventId = eventId });
    }

    public async Task<EventRsvp?> GetEventRsvpAsync(Guid eventId, Guid childId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<EventRsvp>(
            "SELECT * FROM event_rsvps WHERE event_id = @EventId AND child_id = @ChildId", 
            new { EventId = eventId, ChildId = childId }
        );
    }
}
