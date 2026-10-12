using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ChildDailyTimelineItem
{
    public string TimeLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Attendance, Experience, Moment
    public string? Status { get; set; }
    public bool IsCompleted { get; set; }
    public string? PhotoUrl { get; set; }
}

public class ChildDailySummary
{
    public ParentAuthorizedChild Child { get; set; } = new();
    public DateTime Date { get; set; }
    public ChildAttendance? Attendance { get; set; }
    public List<ChildDailyTimelineItem> TimelineItems { get; set; } = new();
    public List<ChildJourneyEntry> ApprovedMoments { get; set; } = new();
    public List<Event> UpcomingEvents { get; set; } = new();
    public ParentDailyCareReport? DailyCareReport { get; set; }
}

public interface IParentService
{
    Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(CancellationToken cancellationToken = default);
    Task<ChildDailySummary> GetChildDailySummaryAsync(Guid childId, DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildJourneyEntry>> GetApprovedChildJourneyAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Event>> GetApprovedEventsAsync(Guid childId, CancellationToken cancellationToken = default);
    Task SubmitEventRsvpAsync(Guid eventId, Guid childId, string status, string? notes, CancellationToken cancellationToken = default);
    Task<Person?> GetParentProfileAsync(CancellationToken cancellationToken = default);
    Task UpdateParentContactAsync(string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken cancellationToken = default);
}

public class ParentService : IParentService
{
    private readonly IParentRepository _parentRepository;
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IChildDailyCareService? _dailyCareService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;

    public ParentService(
        IParentRepository parentRepository,
        IExperienceScheduleRepository scheduleRepository,
        IExperienceRepository experienceRepository,
        IEventRepository eventRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        IChildDailyCareService? dailyCareService = null)
    {
        _parentRepository = parentRepository;
        _scheduleRepository = scheduleRepository;
        _experienceRepository = experienceRepository;
        _eventRepository = eventRepository;
        _dailyCareService = dailyCareService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
    }

    private async Task ValidateAccessOrThrow(Guid childId, CancellationToken cancellationToken)
    {
        var hasAccess = await _parentRepository.HasChildAccessAsync(_tenantContext.TenantId, _userContext.UserId, childId, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException($"User does not have an active authorized relationship with child {childId}.");
        }
    }

    public async Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(CancellationToken cancellationToken = default)
    {
        return await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
    }

    public async Task<ChildDailySummary> GetChildDailySummaryAsync(Guid childId, DateTime date, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");

        var attendance = await _parentRepository.GetChildAttendanceForDateAsync(_tenantContext.TenantId, childId, date, cancellationToken);
        var approvedMoments = (await _parentRepository.GetApprovedJourneyEntriesAsync(_tenantContext.TenantId, childId, cancellationToken))
            .Where(m => m.ObservationDate.Date == date.Date)
            .ToList();

        var upcomingEvents = (await _parentRepository.GetApprovedEventsForChildAsync(_tenantContext.TenantId, childId, cancellationToken))
            .Where(e => e.EventDate >= DateOnly.FromDateTime(date.Date))
            .Take(3)
            .ToList();

        var timelineItems = new List<ChildDailyTimelineItem>();

        // 1. Attendance Timeline Item
        if (attendance != null)
        {
            var timeStr = attendance.ArrivalTime.HasValue ? DateTime.Today.Add(attendance.ArrivalTime.Value).ToString("h:mm tt") : "Morning";
            var desc = attendance.Status == "Present" 
                ? $"Arrived at {timeStr}" + (attendance.DepartureTime.HasValue ? $", departed at {DateTime.Today.Add(attendance.DepartureTime.Value):h:mm tt}" : " · Currently in classroom")
                : $"Status: {attendance.Status}";

            timelineItems.Add(new ChildDailyTimelineItem
            {
                TimeLabel = timeStr,
                Title = "Classroom Check-In",
                Description = desc,
                Category = "Attendance",
                Status = attendance.Status,
                IsCompleted = true
            });
        }

        // 2. Scheduled Classroom Experiences
        if (child.ClassroomId.HasValue)
        {
            var dateOnly = DateOnly.FromDateTime(date.Date);
            var schedules = await _scheduleRepository.GetByClassroomAndDateRangeAsync(child.ClassroomId.Value, dateOnly, dateOnly);
            foreach (var sch in schedules)
            {
                var exp = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, sch.ExperienceId, cancellationToken);
                var isCompleted = string.Equals(sch.Status, "Completed", StringComparison.OrdinalIgnoreCase);
                timelineItems.Add(new ChildDailyTimelineItem
                {
                    TimeLabel = sch.TimeOfDay ?? "Classroom Plan",
                    Title = exp?.Title ?? "Classroom Activity",
                    Description = exp?.Description ?? "Engaging classroom learning experience",
                    Category = "Experience",
                    Status = sch.Status,
                    IsCompleted = isCompleted
                });
            }
        }

        // 3. Approved Moments for today
        foreach (var moment in approvedMoments)
        {
            timelineItems.Add(new ChildDailyTimelineItem
            {
                TimeLabel = moment.ObservationDate.ToString("h:mm tt"),
                Title = moment.Title ?? "Special Moment",
                Description = moment.Observation,
                Category = "Moment",
                Status = "Approved",
                IsCompleted = true
            });
        }

        // 4. Daily Care & Wellbeing Events (Parent Visible)
        ParentDailyCareReport? careReport = null;
        if (_dailyCareService != null)
        {
            try
            {
                careReport = await _dailyCareService.GetParentDailyCareReportAsync(childId, DateOnly.FromDateTime(date), cancellationToken);
                foreach (var careItem in careReport.Timeline)
                {
                    timelineItems.Add(new ChildDailyTimelineItem
                    {
                        TimeLabel = careItem.TimeLabel,
                        Title = careItem.Title,
                        Description = careItem.Description,
                        Category = careItem.Category,
                        Status = "Completed",
                        IsCompleted = true
                    });
                }
            }
            catch
            {
                // Fallback gracefully if care records not yet logged
            }
        }

        return new ChildDailySummary
        {
            Child = child,
            Date = date,
            Attendance = attendance,
            TimelineItems = timelineItems,
            ApprovedMoments = approvedMoments,
            UpcomingEvents = upcomingEvents,
            DailyCareReport = careReport
        };
    }

    public async Task<IEnumerable<ChildJourneyEntry>> GetApprovedChildJourneyAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        return await _parentRepository.GetApprovedJourneyEntriesAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        return await _parentRepository.GetApprovedMilestonesAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task<IEnumerable<Event>> GetApprovedEventsAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        return await _parentRepository.GetApprovedEventsForChildAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task SubmitEventRsvpAsync(Guid eventId, Guid childId, string status, string? notes, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        // Security Validation: Verify the event is published and in scope for the child's organization/classroom
        var approvedEvents = await _parentRepository.GetApprovedEventsForChildAsync(_tenantContext.TenantId, childId, cancellationToken);
        var targetEvent = approvedEvents.FirstOrDefault(e => e.Id == eventId);
        if (targetEvent == null)
        {
            throw new UnauthorizedAccessException($"Event {eventId} is either unpublished, does not exist in this tenant, or is not open to child {childId}.");
        }

        var existingRsvp = await _eventRepository.GetEventRsvpAsync(eventId, childId);
        if (existingRsvp != null)
        {
            existingRsvp.Status = status;
            existingRsvp.Notes = notes;
            existingRsvp.UpdatedAt = DateTimeOffset.UtcNow;
            await _eventRepository.UpdateEventRsvpAsync(existingRsvp);
        }
        else
        {
            var newRsvp = new EventRsvp
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                ChildId = childId,
                GuardianId = _userContext.UserId,
                Status = status,
                Notes = notes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _eventRepository.CreateEventRsvpAsync(newRsvp);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "EventRsvpSubmitted",
            EntityType = "EventRsvp",
            EntityId = eventId,
            Details = new { ChildId = childId, Status = status }
        }, cancellationToken);
    }

    public async Task<Person?> GetParentProfileAsync(CancellationToken cancellationToken = default)
    {
        return await _parentRepository.GetPersonByUserIdAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
    }

    public async Task UpdateParentContactAsync(string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken cancellationToken = default)
    {
        var person = await _parentRepository.GetPersonByUserIdAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        if (person == null)
            throw new InvalidOperationException("Parent profile not found.");

        // Input whitelist validation
        var validMethods = new[] { "Mobile", "Email", "None" };
        var sanitizedMethod = validMethods.Contains(preferredContactMethod, StringComparer.OrdinalIgnoreCase) 
            ? preferredContactMethod 
            : "Mobile";

        var sanitizedLang = string.IsNullOrWhiteSpace(preferredLanguage) || preferredLanguage.Length > 10 ? "en" : preferredLanguage;
        var sanitizedPhone = mobilePhone?.Trim();
        if (sanitizedPhone != null && sanitizedPhone.Length > 50)
            sanitizedPhone = sanitizedPhone.Substring(0, 50);

        await _parentRepository.UpdatePersonContactAsync(_tenantContext.TenantId, person.Id, sanitizedPhone, sanitizedMethod, sanitizedLang, cancellationToken);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentProfileUpdated",
            EntityType = "Person",
            EntityId = person.Id,
            Details = new { mobilePhone = sanitizedPhone, preferredContactMethod = sanitizedMethod, preferredLanguage = sanitizedLang }
        }, cancellationToken);
    }
}
