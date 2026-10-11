using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Event>> GetAllForTenantAsync(Guid tenantId);
    Task<IEnumerable<Event>> GetByBranchAsync(Guid tenantId, Guid branchId);
    Task<IEnumerable<Event>> GetByClassroomAsync(Guid tenantId, Guid classroomId);
    Task<IEnumerable<Event>> GetEventsForDateAsync(Guid tenantId, DateOnly date, Guid? branchId = null, Guid? classroomId = null);
    Task<Guid> CreateAsync(Event evt);
    Task UpdateAsync(Event evt);
    Task DeleteAsync(Guid tenantId, Guid id);
    
    // Event Types
    Task<IEnumerable<EventType>> GetEventTypesAsync(Guid tenantId);
    Task<Guid> CreateEventTypeAsync(EventType eventType);

    // Event Classrooms
    Task AddEventClassroomAsync(EventClassroom eventClassroom);
    Task RemoveEventClassroomAsync(Guid eventId, Guid classroomId);
    Task<IEnumerable<Guid>> GetEventClassroomsAsync(Guid eventId);

    // Event Experiences
    Task AddEventExperienceAsync(EventExperience eventExperience);
    Task RemoveEventExperienceAsync(Guid id);
    Task<IEnumerable<EventExperience>> GetEventExperiencesAsync(Guid eventId);

    // Event Tasks
    Task<Guid> CreateEventTaskAsync(EventTask task);
    Task UpdateEventTaskAsync(EventTask task);
    Task DeleteEventTaskAsync(Guid id);
    Task<IEnumerable<EventTask>> GetEventTasksAsync(Guid eventId);

    // Event RSVPs
    Task<Guid> CreateEventRsvpAsync(EventRsvp rsvp);
    Task UpdateEventRsvpAsync(EventRsvp rsvp);
    Task<IEnumerable<EventRsvp>> GetEventRsvpsAsync(Guid eventId);
    Task<EventRsvp?> GetEventRsvpAsync(Guid eventId, Guid childId);

    // Event Photos
    Task UpdatePhotosAsync(Guid tenantId, Guid eventId, string[] photoUrls);
}
