using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Audit;

namespace EnjoyEveryday.Application.Services;

public class EventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IChildRepository _childRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;

    public EventService(
        IEventRepository eventRepository,
        IChildRepository childRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService)
    {
        _eventRepository = eventRepository;
        _childRepository = childRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
    }

    public async Task<IEnumerable<Event>> GetEventsAsync()
    {
        var tenantId = _tenantContext.TenantId;
        return await _eventRepository.GetAllForTenantAsync(tenantId);
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        return await _eventRepository.GetByIdAsync(tenantId, id);
    }

    public async Task<Guid> CreateEventAsync(Event evt)
    {
        if (!_userContext.HasPermission("event.create")) 
            throw new UnauthorizedAccessException("Requires event.create permission.");

        if (string.IsNullOrWhiteSpace(evt.Status))
        {
            evt.Status = "Draft";
        }

        evt.TenantId = _tenantContext.TenantId;
        evt.CreatedAt = DateTimeOffset.UtcNow;
        evt.UpdatedAt = DateTimeOffset.UtcNow;

        await ValidateEventConflictsAsync(evt, excludeEventId: null);

        var id = await _eventRepository.CreateAsync(evt);
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = evt.TenantId,
            UserId = _userContext.UserId,
            Action = "Event.Created",
            EntityType = "Event",
            EntityId = evt.Id,
            Details = $"Event '{evt.Title}' created."
        });
        
        return id;
    }

    public async Task UpdateEventAsync(Event evt)
    {
        if (!_userContext.HasPermission("event.manage")) 
            throw new UnauthorizedAccessException("Requires event.manage permission.");

        evt.TenantId = _tenantContext.TenantId;
        // Cancelling always frees the slot, so it must never be blocked by a conflict check.
        if (!string.Equals(evt.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            await ValidateEventConflictsAsync(evt, excludeEventId: evt.Id);
        }

        try
        {
            await _eventRepository.UpdateAsync(evt);
        }
        catch (Exception ex) when (SchedulingRuleException.TryTranslate(ex, out var rule))
        {
            throw new InvalidOperationException(rule.Message, ex);
        }
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = evt.TenantId,
            UserId = _userContext.UserId,
            Action = "Event.Updated",
            EntityType = "Event",
            EntityId = evt.Id,
            Details = $"Event '{evt.Title}' updated."
        });
    }

    public async Task ValidateEventConflictsAsync(Event evt, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        if (evt.StartTime.HasValue && evt.EndTime.HasValue && evt.EndTime <= evt.StartTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        var tenantId = _tenantContext.TenantId;
        var existingEvents = (await _eventRepository.GetEventsForDateAsync(tenantId, evt.EventDate, null, null)).ToList();

        foreach (var existing in existingEvents)
        {
            if (excludeEventId.HasValue && existing.Id == excludeEventId.Value) continue;
            if (existing.Id == evt.Id) continue;
            if (string.Equals(existing.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) continue;

            // Check time overlap
            bool timeOverlaps = false;
            if (evt.StartTime.HasValue && evt.EndTime.HasValue && existing.StartTime.HasValue && existing.EndTime.HasValue)
            {
                timeOverlaps = evt.StartTime < existing.EndTime && evt.EndTime > existing.StartTime;
            }
            else
            {
                // When an event has no specific start/end bounds (e.g. all-day celebration), it spans the day and overlaps
                timeOverlaps = true;
            }

            if (!timeOverlaps) continue;

            // Check scope overlap
            bool scopeConflicts = false;
            if (evt.Scope == "Organization" || evt.IsOrganizationWide || existing.Scope == "Organization" || existing.IsOrganizationWide)
            {
                scopeConflicts = true;
            }
            else if (evt.BranchId.HasValue && existing.BranchId.HasValue)
            {
                scopeConflicts = (evt.BranchId == existing.BranchId);
            }
            else
            {
                scopeConflicts = true;
            }

            if (scopeConflicts)
            {
                var timeDisplay = (existing.StartTime.HasValue && existing.EndTime.HasValue)
                    ? $"{existing.StartTime.Value:hh\\:mm} - {existing.EndTime.Value:hh\\:mm}"
                    : "all-day";
                throw new InvalidOperationException($"Another event ('{existing.Title}', {timeDisplay}) is already planned during this time period on {existing.EventDate:yyyy-MM-dd}. No other event can be planned at the same time.");
            }
        }
    }

    public async Task DeleteEventAsync(Guid id)
    {
        if (!_userContext.HasPermission("event.manage")) 
            throw new UnauthorizedAccessException("Requires event.manage permission.");

        var tenantId = _tenantContext.TenantId;
        await _eventRepository.DeleteAsync(tenantId, id);
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "Event.Deleted",
            EntityType = "Event",
            EntityId = id,
            Details = $"Event '{id}' deleted."
        });
    }

    public async Task<IEnumerable<Event>> GetEventsByClassroomAsync(Guid classroomId)
    {
        var tenantId = _tenantContext.TenantId;
        return await _eventRepository.GetByClassroomAsync(tenantId, classroomId);
    }

    public async Task<IEnumerable<Event>> GetEventsByBranchAsync(Guid branchId)
    {
        var tenantId = _tenantContext.TenantId;
        return await _eventRepository.GetByBranchAsync(tenantId, branchId);
    }

    public async Task<IEnumerable<Event>> GetEventsForDateAsync(DateOnly date, Guid? branchId = null, Guid? classroomId = null)
    {
        var tenantId = _tenantContext.TenantId;
        return await _eventRepository.GetEventsForDateAsync(tenantId, date, branchId, classroomId);
    }

    public async Task<IEnumerable<Guid>> GetEventClassroomsAsync(Guid eventId)
    {
        return await _eventRepository.GetEventClassroomsAsync(eventId);
    }

    public async Task RemoveClassroomFromEventAsync(Guid eventId, Guid classroomId)
    {
        if (!_userContext.HasPermission("event.manage")) throw new UnauthorizedAccessException();
        await _eventRepository.RemoveEventClassroomAsync(eventId, classroomId);
    }

    public async Task UpdateStatusAsync(Guid eventId, string newStatus, string? exceptionReason = null)
    {
        var evt = await GetEventByIdAsync(eventId);
        if (evt == null) throw new ArgumentException("Event not found");

        if (newStatus == "Approved" || newStatus == "Published" || newStatus == "Cancelled")
        {
            if (!_userContext.HasPermission($"event.{newStatus.ToLower()}"))
                throw new UnauthorizedAccessException($"Requires event.{newStatus.ToLower()} permission.");
        }

        if (newStatus == "Published" || newStatus == "Completed")
        {
            // Safety check validation
            var isSafe = await ValidateEventSafetyAsync(eventId);
            if (!isSafe && string.IsNullOrEmpty(exceptionReason))
            {
                throw new InvalidOperationException("Cannot transition state. Safety checks (allergies/consent) have not been cleared or explicitly overridden.");
            }
            if (!isSafe && !string.IsNullOrEmpty(exceptionReason))
            {
                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = evt.TenantId,
                    UserId = _userContext.UserId,
                    Action = "Event.SafetyException",
                    EntityType = "Event",
                    EntityId = eventId,
                    Details = $"Safety override provided: {exceptionReason}"
                });
            }
        }

        evt.Status = newStatus;
        await UpdateEventAsync(evt);
    }

    private async Task<bool> ValidateEventSafetyAsync(Guid eventId)
    {
        // Placeholder for complex business logic checking ChildAllergy, ChildDietaryProfile, ChildConsent
        // For MVP, we assume it's true unless there's some explicit flag we add later
        return await Task.FromResult(true); 
    }

    public async Task AddClassroomToEventAsync(Guid eventId, Guid classroomId)
    {
        if (!_userContext.HasPermission("event.manage")) throw new UnauthorizedAccessException();
        try
        {
            // DB trigger enforces: one non-cancelled activity per classroom at any point in time.
            await _eventRepository.AddEventClassroomAsync(new EventClassroom { EventId = eventId, ClassroomId = classroomId });
        }
        catch (Exception ex) when (SchedulingRuleException.TryTranslate(ex, out var rule))
        {
            throw new InvalidOperationException(rule.Message, ex);
        }
    }

    public async Task AddExperienceToEventAsync(Guid eventId, Guid experienceId, Guid? scheduleId, Guid? sessionId)
    {
        if (!_userContext.HasPermission("event.manage")) throw new UnauthorizedAccessException();
        await _eventRepository.AddEventExperienceAsync(new EventExperience 
        { 
            EventId = eventId, 
            ExperienceId = experienceId,
            ExperienceScheduleId = scheduleId,
            ExperienceSessionId = sessionId
        });
    }

    // Tasks
    public async Task<Guid> CreateTaskAsync(EventTask task)
    {
        if (!_userContext.HasPermission("event.manage")) throw new UnauthorizedAccessException();
        return await _eventRepository.CreateEventTaskAsync(task);
    }

    public async Task<IEnumerable<EventTask>> GetTasksAsync(Guid eventId)
    {
        return await _eventRepository.GetEventTasksAsync(eventId);
    }

    public async Task UpdateTaskAsync(EventTask task)
    {
        if (!_userContext.HasPermission("event.manage")) throw new UnauthorizedAccessException();
        await _eventRepository.UpdateEventTaskAsync(task);
    }

    // RSVPs
    public async Task<Guid> SubmitRsvpAsync(EventRsvp rsvp)
    {
        // We might want to check if the user is the guardian of the child, but for now we enforce tenant via service logic
        var existing = await _eventRepository.GetEventRsvpAsync(rsvp.EventId, rsvp.ChildId);
        if (existing != null)
        {
            existing.Status = rsvp.Status;
            existing.Notes = rsvp.Notes;
            await _eventRepository.UpdateEventRsvpAsync(existing);
            return existing.Id;
        }
        else
        {
            return await _eventRepository.CreateEventRsvpAsync(rsvp);
        }
    }

    // Event Photos
    public async Task AddEventPhotoAsync(Guid eventId, string photoUrl)
    {
        var evt = await GetEventByIdAsync(eventId);
        if (evt == null) throw new ArgumentException("Event not found");

        if (!string.Equals(evt.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Photos can only be added to completed events.");
        }

        var currentPhotos = (evt.PhotoUrls ?? Array.Empty<string>()).ToList();
        if (currentPhotos.Count >= 3)
        {
            throw new InvalidOperationException("A maximum of 3 photos is allowed per event.");
        }

        currentPhotos.Add(photoUrl);
        await _eventRepository.UpdatePhotosAsync(_tenantContext.TenantId, eventId, currentPhotos.ToArray());

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = evt.TenantId,
            UserId = _userContext.UserId,
            Action = "Event.PhotoAdded",
            EntityType = "Event",
            EntityId = eventId,
            Details = $"Added photo to event '{evt.Title}'."
        });
    }

    public async Task RemoveEventPhotoAsync(Guid eventId, string photoUrl)
    {
        var evt = await GetEventByIdAsync(eventId);
        if (evt == null) throw new ArgumentException("Event not found");

        var currentPhotos = (evt.PhotoUrls ?? Array.Empty<string>()).ToList();
        currentPhotos.Remove(photoUrl);
        await _eventRepository.UpdatePhotosAsync(_tenantContext.TenantId, eventId, currentPhotos.ToArray());
    }

    public async Task UpdateEventPhotosAsync(Guid eventId, string[] photoUrls)
    {
        var evt = await GetEventByIdAsync(eventId);
        if (evt == null) throw new ArgumentException("Event not found");

        if (!string.Equals(evt.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Photos can only be added to completed events.");
        }

        if (photoUrls != null && photoUrls.Length > 3)
        {
            throw new InvalidOperationException("A maximum of 3 photos is allowed per event.");
        }

        await _eventRepository.UpdatePhotosAsync(_tenantContext.TenantId, eventId, photoUrls ?? Array.Empty<string>());
    }
}
