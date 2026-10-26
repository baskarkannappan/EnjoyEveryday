using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EventRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] CreateEventParams =
    [
        "Id", "TenantId", "Title", "Description", "EventTypeId", "EventDate", "StartTime", "EndTime",
        "Location", "OrganizerId", "Status", "Scope", "BranchId", "IsOrganizationWide", "PhotoUrls",
        "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateEventParams =
    [
        "Id", "TenantId", "Title", "Description", "EventTypeId", "EventDate", "StartTime", "EndTime",
        "Location", "OrganizerId", "Status", "Scope", "BranchId", "IsOrganizationWide", "PhotoUrls",
        "UpdatedAt"
    ];

    private static readonly string[] CreateEventTypeParams =
    [
        "Id", "TenantId", "Name", "Description", "IsActive", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] AddClassroomParams =
    [
        "EventId", "ClassroomId"
    ];

    private static readonly string[] AddExperienceParams =
    [
        "Id", "EventId", "ExperienceId", "ExperienceScheduleId", "ExperienceSessionId"
    ];

    private static readonly string[] CreateTaskParams =
    [
        "Id", "EventId", "Description", "OwnerId", "DueDate", "IsCompleted", "CompletedAt", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateTaskParams =
    [
        "Id", "Description", "OwnerId", "DueDate", "IsCompleted", "CompletedAt", "UpdatedAt"
    ];

    private static readonly string[] CreateRsvpParams =
    [
        "Id", "EventId", "ChildId", "GuardianId", "Status", "Notes", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateRsvpParams =
    [
        "Id", "Status", "Notes", "UpdatedAt"
    ];

    public async Task<Event?> GetByIdAsync(Guid tenantId, Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleFunctionAsync<Event>(
            "public.fn_get_event_by_id",
            new { TenantId = tenantId, Id = id });
    }

    public async Task<IEnumerable<Event>> GetAllForTenantAsync(Guid tenantId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<Event>(
            "public.fn_get_all_events_for_tenant",
            new { TenantId = tenantId });
    }

    public async Task<IEnumerable<Event>> GetByBranchAsync(Guid tenantId, Guid branchId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<Event>(
            "public.fn_get_events_by_branch",
            new { TenantId = tenantId, BranchId = branchId });
    }

    public async Task<IEnumerable<Event>> GetByClassroomAsync(Guid tenantId, Guid classroomId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<Event>(
            "public.fn_get_events_by_classroom",
            new { TenantId = tenantId, ClassroomId = classroomId });
    }

    public async Task<IEnumerable<Event>> GetEventsForDateAsync(Guid tenantId, DateOnly date, Guid? branchId = null, Guid? classroomId = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<Event>(
            "public.fn_get_events_for_date",
            new
            {
                TenantId = tenantId,
                Date = date.ToDateTime(TimeOnly.MinValue),
                BranchId = branchId,
                ClassroomId = classroomId
            });
    }

    public async Task<Guid> CreateAsync(Event evt)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarFunctionAsync<Guid>(
            "public.fn_create_event",
            evt,
            parameterNames: CreateEventParams);
    }

    public async Task UpdateAsync(Event evt)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        evt.UpdatedAt = DateTimeOffset.UtcNow;
        await connection.ExecuteProcedureAsync(
            "public.sp_update_event",
            evt,
            parameterNames: UpdateEventParams);
    }

    public async Task UpdatePhotosAsync(Guid tenantId, Guid eventId, string[] photoUrls)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_update_event_photos",
            new { Id = eventId, TenantId = tenantId, PhotoUrls = photoUrls, UpdatedAt = DateTimeOffset.UtcNow });
    }

    public async Task DeleteAsync(Guid tenantId, Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_event",
            new { TenantId = tenantId, Id = id });
    }

    public async Task<IEnumerable<EventType>> GetEventTypesAsync(Guid tenantId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<EventType>(
            "public.fn_get_event_types",
            new { TenantId = tenantId });
    }

    public async Task<Guid> CreateEventTypeAsync(EventType eventType)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarFunctionAsync<Guid>(
            "public.fn_create_event_type",
            eventType,
            parameterNames: CreateEventTypeParams);
    }

    public async Task AddEventClassroomAsync(EventClassroom eventClassroom)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_add_event_classroom",
            eventClassroom,
            parameterNames: AddClassroomParams);
    }

    public async Task RemoveEventClassroomAsync(Guid eventId, Guid classroomId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_remove_event_classroom",
            new { EventId = eventId, ClassroomId = classroomId });
    }

    public async Task<IEnumerable<Guid>> GetEventClassroomsAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<Guid>(
            "public.fn_get_event_classrooms",
            new { EventId = eventId });
    }

    public async Task AddEventExperienceAsync(EventExperience eventExperience)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_add_event_experience",
            eventExperience,
            parameterNames: AddExperienceParams);
    }

    public async Task RemoveEventExperienceAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_remove_event_experience",
            new { Id = id });
    }

    public async Task<IEnumerable<EventExperience>> GetEventExperiencesAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<EventExperience>(
            "public.fn_get_event_experiences",
            new { EventId = eventId });
    }

    public async Task<Guid> CreateEventTaskAsync(EventTask task)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarFunctionAsync<Guid>(
            "public.fn_create_event_task",
            task,
            parameterNames: CreateTaskParams);
    }

    public async Task UpdateEventTaskAsync(EventTask task)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await connection.ExecuteProcedureAsync(
            "public.sp_update_event_task",
            task,
            parameterNames: UpdateTaskParams);
    }

    public async Task DeleteEventTaskAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_event_task",
            new { Id = id });
    }

    public async Task<IEnumerable<EventTask>> GetEventTasksAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<EventTask>(
            "public.fn_get_event_tasks",
            new { EventId = eventId });
    }

    public async Task<Guid> CreateEventRsvpAsync(EventRsvp rsvp)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarFunctionAsync<Guid>(
            "public.fn_create_event_rsvp",
            rsvp,
            parameterNames: CreateRsvpParams);
    }

    public async Task UpdateEventRsvpAsync(EventRsvp rsvp)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        rsvp.UpdatedAt = DateTimeOffset.UtcNow;
        await connection.ExecuteProcedureAsync(
            "public.sp_update_event_rsvp",
            rsvp,
            parameterNames: UpdateRsvpParams);
    }

    public async Task<IEnumerable<EventRsvp>> GetEventRsvpsAsync(Guid eventId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFunctionAsync<EventRsvp>(
            "public.fn_get_event_rsvps",
            new { EventId = eventId });
    }

    public async Task<EventRsvp?> GetEventRsvpAsync(Guid eventId, Guid childId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QuerySingleFunctionAsync<EventRsvp>(
            "public.fn_get_event_rsvp",
            new { EventId = eventId, ChildId = childId });
    }
}
