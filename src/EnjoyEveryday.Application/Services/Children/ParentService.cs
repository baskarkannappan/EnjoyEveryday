using EnjoyEveryday.Domain.Constants;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;


public partial class ParentService : IParentService
{
    private readonly IParentRepository _parentRepository;
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IChildDailyCareService? _dailyCareService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly IParentCommunicationRepository? _communicationRepository;
    private readonly ISecurePickupService? _pickupService;
    private readonly IBillingService? _billingService;
    private readonly IConsentService? _consentService;
    private readonly IRealtimeNotificationService? _realtimeNotificationService;
    private readonly IRazorpayGateway? _razorpayGateway;
    private readonly IOrgFeeRepository? _orgFeeRepository;

    public ParentService(
        IParentRepository parentRepository,
        IExperienceScheduleRepository scheduleRepository,
        IExperienceRepository experienceRepository,
        IEventRepository eventRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        IChildDailyCareService? dailyCareService = null,
        IParentCommunicationRepository? communicationRepository = null,
        ISecurePickupService? pickupService = null,
        IBillingService? billingService = null,
        IConsentService? consentService = null,
        IRealtimeNotificationService? realtimeNotificationService = null,
        IRazorpayGateway? razorpayGateway = null,
        IOrgFeeRepository? orgFeeRepository = null)
    {
        _parentRepository = parentRepository;
        _scheduleRepository = scheduleRepository;
        _experienceRepository = experienceRepository;
        _eventRepository = eventRepository;
        _dailyCareService = dailyCareService;
        _communicationRepository = communicationRepository;
        _pickupService = pickupService;
        _billingService = billingService;
        _consentService = consentService;
        _realtimeNotificationService = realtimeNotificationService;
        _razorpayGateway = razorpayGateway;
        _orgFeeRepository = orgFeeRepository;
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

        var effectiveTenantId = (child.TenantId != Guid.Empty) ? child.TenantId : _tenantContext.TenantId;
        var targetDate = DateOnly.FromDateTime(date);

        var attendance = await _parentRepository.GetChildAttendanceForDateAsync(effectiveTenantId, childId, date, cancellationToken);
        var allApprovedMoments = await _parentRepository.GetApprovedJourneyEntriesAsync(effectiveTenantId, childId, cancellationToken);
        var approvedMoments = allApprovedMoments
            .Where(m => DateOnly.FromDateTime(m.ObservationDate.Date) == targetDate
                     || DateOnly.FromDateTime(m.ObservationDate.LocalDateTime) == targetDate
                     || DateOnly.FromDateTime(m.ObservationDate.UtcDateTime) == targetDate)
            .ToList();

        var upcomingEvents = (await _parentRepository.GetApprovedEventsForChildAsync(effectiveTenantId, childId, cancellationToken))
            .Where(e => e.EventDate >= targetDate)
            .Take(3)
            .ToList();

        var timelineItems = new List<ChildDailyTimelineItem>();
        DateTime baseDateTime = targetDate.ToDateTime(TimeOnly.MinValue);

        // 1. Attendance Timeline Item
        if (attendance != null && attendance.ArrivalTime.HasValue)
        {
            var arrTime = baseDateTime.Add(attendance.ArrivalTime.Value);
            var timeStr = arrTime.ToString("h:mm tt");
            var desc = attendance.Status == "Present" 
                ? $"Arrived at {timeStr}" + (attendance.DepartureTime.HasValue ? $", departed at {baseDateTime.Add(attendance.DepartureTime.Value):h:mm tt}" : " · Currently in classroom")
                : $"Status: {attendance.Status}";

            timelineItems.Add(new ChildDailyTimelineItem
            {
                EventTime = arrTime,
                TimeLabel = timeStr,
                Title = "Classroom Check-In",
                Description = desc,
                Category = "Attendance",
                Icon = "☀️",
                Status = attendance.Status,
                IsCompleted = true
            });

            if (attendance.DepartureTime.HasValue)
            {
                var depTime = baseDateTime.Add(attendance.DepartureTime.Value);
                timelineItems.Add(new ChildDailyTimelineItem
                {
                    EventTime = depTime,
                    TimeLabel = depTime.ToString("h:mm tt"),
                    Title = "Classroom Departure / Check-Out",
                    Description = !string.IsNullOrWhiteSpace(attendance.PickupPersonName)
                        ? $"Picked up by {attendance.PickupPersonName} ({attendance.PickupPersonRelationship ?? "Authorized Contact"}). Handover verified."
                        : "Checked out and safely departed.",
                    Category = "Attendance",
                    Icon = "🚪",
                    Status = "Completed",
                    IsCompleted = true
                });
            }
        }

        // 2. Scheduled Classroom Experiences
        ExperienceHighlightDto? highlight = null;
        if (child.ClassroomId.HasValue)
        {
            var schedules = await _scheduleRepository.GetByClassroomAndDateRangeAsync(child.ClassroomId.Value, targetDate, targetDate);
            foreach (var sch in schedules)
            {
                var exp = await _experienceRepository.GetByIdAsync(effectiveTenantId, sch.ExperienceId, cancellationToken);
                var isCompleted = string.Equals(sch.Status, "Completed", StringComparison.OrdinalIgnoreCase);
                var expTime = sch.PlannedStartTime.HasValue 
                    ? baseDateTime.Add(sch.PlannedStartTime.Value) 
                    : (string.Equals(sch.TimeOfDay, "Afternoon", StringComparison.OrdinalIgnoreCase) ? baseDateTime.AddHours(14) : baseDateTime.AddHours(10));
                var timeLabel = sch.PlannedStartTime.HasValue 
                    ? expTime.ToString("h:mm tt") 
                    : (sch.TimeOfDay ?? "Classroom Plan");

                timelineItems.Add(new ChildDailyTimelineItem
                {
                    EventTime = expTime,
                    TimeLabel = timeLabel,
                    Title = exp?.Title ?? "Classroom Activity",
                    Description = exp?.Description ?? "Engaging classroom learning experience",
                    Category = "Experience",
                    Icon = "🎨",
                    Status = sch.Status,
                    IsCompleted = isCompleted
                });

                if (highlight == null && exp != null)
                {
                    ExperienceDna? dna = null;
                    if (!string.IsNullOrWhiteSpace(exp.DnaPayload))
                    {
                        try
                        {
                            dna = System.Text.Json.JsonSerializer.Deserialize<ExperienceDna>(exp.DnaPayload, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        catch { }
                    }

                    var ask = dna?.ExecutionGuide?.Ask;
                    var starter = !string.IsNullOrWhiteSpace(ask) 
                        ? $"Ask {child.FirstName}: \"{ask}\"" 
                        : $"Ask {child.FirstName} what they created in '{exp.Title}' today!";
                    var homeExtension = dna?.ExecutionGuide?.Extensions?.FirstOrDefault() 
                        ?? (!string.IsNullOrWhiteSpace(dna?.ExecutionGuide?.Try) ? dna!.ExecutionGuide.Try : $"Ask {child.FirstName} to show you their favorite game or exploration from today's {exp.Title}!");

                    highlight = new ExperienceHighlightDto
                    {
                        ExperienceId = exp.Id,
                        Title = exp.Title,
                        Description = exp.Description,
                        MagicMoment = dna?.MagicMoment ?? string.Empty,
                        BedtimeConversationStarter = starter,
                        TryAtHomeExtension = homeExtension,
                        Materials = dna?.Materials ?? new List<string>(),
                        DevelopmentalGoals = dna?.DevelopmentalGoals ?? new List<string>(),
                        TimeLabel = timeLabel,
                        IsCompleted = isCompleted,
                        PhotoUrl = approvedMoments.FirstOrDefault(m => m.Photos?.Any() == true)?.Photos?.FirstOrDefault()
                    };
                }
            }
        }

        // 3. Approved Moments for today
        foreach (var moment in approvedMoments)
        {
            var mTime = moment.ObservationDate != default ? moment.ObservationDate.LocalDateTime : baseDateTime.AddHours(11);
            timelineItems.Add(new ChildDailyTimelineItem
            {
                EventTime = mTime,
                TimeLabel = mTime.ToString("h:mm tt"),
                Title = moment.Title ?? "Special Moment",
                Description = moment.Observation,
                Category = "Moment",
                Icon = "✨",
                Status = "Approved",
                IsCompleted = true,
                PhotoUrl = moment.Photos?.FirstOrDefault()
            });
        }

        // 4. Daily Care & Wellbeing Events (Parent Visible)
        ParentDailyCareReport? careReport = null;
        if (_dailyCareService != null)
        {
            try
            {
                careReport = await _dailyCareService.GetParentDailyCareReportAsync(effectiveTenantId, childId, DateOnly.FromDateTime(date), cancellationToken);
                foreach (var careItem in careReport.Timeline)
                {
                    if (careItem.Category == "Attendance" && timelineItems.Any(t => t.Category == "Attendance" && t.Title == "Classroom Check-In"))
                    {
                        continue;
                    }

                    var careEventTime = careItem.EventTime != default
                        ? (careItem.EventTime.Kind == DateTimeKind.Utc ? careItem.EventTime.ToLocalTime() : careItem.EventTime)
                        : baseDateTime.AddHours(12);

                    timelineItems.Add(new ChildDailyTimelineItem
                    {
                        EventTime = careEventTime,
                        TimeLabel = careEventTime.ToString("h:mm tt"),
                        Title = careItem.Title,
                        Description = careItem.Description,
                        Category = careItem.Category,
                        Icon = careItem.Icon,
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

        // Sort all items strictly into chronological time-series order (normalized to local time)
        timelineItems = timelineItems
            .OrderBy(t => t.EventTime.Kind == DateTimeKind.Utc ? t.EventTime.ToLocalTime() : t.EventTime)
            .ToList();

        var hasCareUpdates = careReport != null && (
            careReport.DiaperChangesCount > 0 ||
            careReport.TotalRestMinutes > 0 ||
            careReport.Timeline.Any(t => t.Category != "Attendance") ||
            (careReport.LunchMealStatus != "Not recorded yet" && careReport.LunchMealStatus != "Meal routine recorded")
        );

        var attendanceStatus = "Not checked in today";
        if (attendance != null)
        {
            if (attendance.DepartureTime.HasValue)
            {
                attendanceStatus = "Checked Out / Departed";
            }
            else if (attendance.ArrivalTime.HasValue)
            {
                attendanceStatus = $"Present in Classroom (arrived {DateTime.Today.Add(attendance.ArrivalTime.Value):h:mm tt})";
            }
            else
            {
                attendanceStatus = "Checked In";
            }
        }

        int unreadMessages = 0;
        if (_communicationRepository != null)
        {
            try
            {
                var messages = await _communicationRepository.GetByChildIdAsync(effectiveTenantId, childId, cancellationToken);
                unreadMessages = messages.Count(m => m.Status != ParentCommunicationStatus.Approved && m.TeacherId != Guid.Empty && m.DeliveryStatus != "Read");
            }
            catch { }
        }

        int pendingConsent = 0;
        if (_consentService != null)
        {
            try
            {
                var consents = await _consentService.GetDocumentsAsync(childId: childId, ct: cancellationToken);
                pendingConsent = consents.Count(c => c.Status == "PendingSignature" || c.Status == "PendingReview");
            }
            catch { }
        }

        int unpaidInvoices = 0;
        if (_billingService != null)
        {
            try
            {
                var invoices = await _billingService.GetAllInvoicesAsync(InvoiceStatus.Unpaid, ct: cancellationToken);
                unpaidInvoices = invoices.Count(i => i.BalanceDue > 0);
            }
            catch { }
        }

        return new ChildDailySummary
        {
            Child = child,
            Date = date,
            Attendance = attendance,
            TimelineItems = timelineItems,
            ApprovedMoments = approvedMoments,
            UpcomingEvents = upcomingEvents,
            DailyCareReport = careReport,
            TodayExperienceHighlight = highlight,
            HasCareUpdatesToday = hasCareUpdates,
            AttendanceStatusLabel = attendanceStatus,
            UnreadMessagesCount = unreadMessages,
            PendingConsentCount = pendingConsent,
            UnpaidInvoicesCount = unpaidInvoices
        };
    }

    private async Task<Guid> ResolveEffectiveTenantIdAsync(Guid childId, CancellationToken cancellationToken)
    {
        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId);
        return (child != null && child.TenantId != Guid.Empty) ? child.TenantId : _tenantContext.TenantId;
    }

    public async Task<IEnumerable<ChildJourneyEntry>> GetApprovedChildJourneyAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var effectiveTenantId = await ResolveEffectiveTenantIdAsync(childId, cancellationToken);
        var entries = (await _parentRepository.GetApprovedJourneyEntriesAsync(effectiveTenantId, childId, cancellationToken)).ToList();

        var attendance = await _parentRepository.GetChildAttendanceForDateAsync(effectiveTenantId, childId, DateTime.Today, cancellationToken);
        if (attendance != null)
        {
            if (attendance.ArrivalTime.HasValue && !entries.Any(e =>
                string.Equals(e.JourneyType, JourneyTypes.Attendance, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.ParticipationType, "Arrival", StringComparison.OrdinalIgnoreCase) &&
                e.ObservationDate.ToLocalTime().Date == DateTime.Today))
            {
                var arrTime = DateTime.Today.Add(attendance.ArrivalTime.Value);
                entries.Add(new ChildJourneyEntry
                {
                    Id = attendance.Id,
                    TenantId = effectiveTenantId,
                    ChildId = childId,
                    JourneyType = JourneyTypes.Attendance,
                    Title = "Classroom Check-In",
                    Observation = $"Checked in at {arrTime:h:mm tt}.",
                    ObservationDate = new DateTimeOffset(arrTime, TimeZoneInfo.Local.GetUtcOffset(arrTime)),
                    ParticipationType = "Arrival",
                    IsParentVisible = true,
                    ParentVisibilityStatus = ParentVisibilityStatuses.Approved
                });
            }

            if (attendance.DepartureTime.HasValue && !entries.Any(e =>
                string.Equals(e.JourneyType, JourneyTypes.Attendance, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.ParticipationType, "Departure", StringComparison.OrdinalIgnoreCase) &&
                e.ObservationDate.ToLocalTime().Date == DateTime.Today))
            {
                var depTime = DateTime.Today.Add(attendance.DepartureTime.Value);
                var pickupDesc = !string.IsNullOrWhiteSpace(attendance.PickupPersonName)
                    ? $"Picked up by {attendance.PickupPersonName} ({attendance.PickupPersonRelationship ?? "Authorized Pickup"}). Handover verified."
                    : "Child departed / checked out safely.";
                entries.Add(new ChildJourneyEntry
                {
                    Id = attendance.Id,
                    TenantId = effectiveTenantId,
                    ChildId = childId,
                    JourneyType = JourneyTypes.Attendance,
                    Title = "Classroom Departure / Check-Out",
                    Observation = $"Checked out at {depTime:h:mm tt}. {pickupDesc}",
                    ObservationDate = new DateTimeOffset(depTime, TimeZoneInfo.Local.GetUtcOffset(depTime)),
                    ParticipationType = "Departure",
                    IsParentVisible = true,
                    ParentVisibilityStatus = ParentVisibilityStatuses.Approved
                });
            }
        }

        return entries.OrderByDescending(e => e.ObservationDate.ToLocalTime());
    }

    public async Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var effectiveTenantId = await ResolveEffectiveTenantIdAsync(childId, cancellationToken);
        return await _parentRepository.GetApprovedMilestonesAsync(effectiveTenantId, childId, cancellationToken);
    }

    public async Task<IEnumerable<Event>> GetApprovedEventsAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var effectiveTenantId = await ResolveEffectiveTenantIdAsync(childId, cancellationToken);
        return await _parentRepository.GetApprovedEventsForChildAsync(effectiveTenantId, childId, cancellationToken);
    }

    public async Task SubmitEventRsvpAsync(Guid eventId, Guid childId, string status, string? notes, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var effectiveTenantId = await ResolveEffectiveTenantIdAsync(childId, cancellationToken);

        // Security Validation: Verify the event is published and in scope for the child's organization/classroom
        var approvedEvents = await _parentRepository.GetApprovedEventsForChildAsync(effectiveTenantId, childId, cancellationToken);
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
            TenantId = effectiveTenantId,
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

    public async Task<IEnumerable<ParentMediaItem>> GetChildMemoriesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var effectiveTenantId = await ResolveEffectiveTenantIdAsync(childId, cancellationToken);

        var mediaList = new List<ParentMediaItem>();

        // 1. Photos from approved classroom events & celebrations
        var events = await _parentRepository.GetApprovedEventsForChildAsync(effectiveTenantId, childId, cancellationToken);
        foreach (var evt in events)
        {
            if (evt.PhotoUrls != null && evt.PhotoUrls.Length > 0)
            {
                for (int i = 0; i < evt.PhotoUrls.Length; i++)
                {
                    var photoUrl = evt.PhotoUrls[i];
                    if (!string.IsNullOrWhiteSpace(photoUrl))
                    {
                        mediaList.Add(new ParentMediaItem
                        {
                            Id = $"{evt.Id}_{i}",
                            Url = photoUrl,
                            ThumbnailUrl = photoUrl,
                            Title = evt.Title,
                            Caption = evt.Description ?? $"Celebration moment from {evt.Title}",
                            Date = evt.EventDate.ToDateTime(TimeOnly.MinValue),
                            Category = "Celebration",
                            IsVideo = false
                        });
                    }
                }
            }
        }

        // 2. Photos and media from approved child journey entries
        var journeyEntries = await _parentRepository.GetApprovedJourneyEntriesAsync(effectiveTenantId, childId, cancellationToken);
        foreach (var entry in journeyEntries)
        {
            if (entry.Photos != null && entry.Photos.Count > 0)
            {
                for (int i = 0; i < entry.Photos.Count; i++)
                {
                    var photoUrl = entry.Photos[i];
                    mediaList.Add(new ParentMediaItem
                    {
                        Id = $"{entry.Id}_{i}",
                        Url = photoUrl,
                        ThumbnailUrl = photoUrl,
                        Title = entry.Title ?? "Learning Observation",
                        Caption = entry.Observation + (!string.IsNullOrEmpty(entry.ChildVoice) ? $" — \"{entry.ChildVoice}\"" : ""),
                        Date = entry.ObservationDate,
                        Category = "Journey",
                        IsVideo = false
                    });
                }
            }
            else if (!string.IsNullOrEmpty(entry.Observation) && !string.Equals(entry.JourneyType, JourneyTypes.Attendance, StringComparison.OrdinalIgnoreCase))
            {
                mediaList.Add(new ParentMediaItem
                {
                    Id = entry.Id.ToString("N"),
                    Url = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='400' height='300' viewBox='0 0 400 300'><rect width='400' height='300' fill='%23e6eee9'/><text x='50%25' y='45%25' dominant-baseline='middle' text-anchor='middle' font-family='sans-serif' font-size='42'>🌿</text><text x='50%25' y='65%25' dominant-baseline='middle' text-anchor='middle' font-family='sans-serif' font-size='14' fill='%233e6253'>Child Learning Journey</text></svg>",
                    ThumbnailUrl = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='200' height='150' viewBox='0 0 200 150'><rect width='200' height='150' fill='%23e6eee9'/><text x='50%25' y='45%25' dominant-baseline='middle' text-anchor='middle' font-family='sans-serif' font-size='28'>🌿</text><text x='50%25' y='68%25' dominant-baseline='middle' text-anchor='middle' font-family='sans-serif' font-size='11' fill='%233e6253'>Observation</text></svg>",
                    Title = entry.Title ?? "Learning Observation",
                    Caption = entry.Observation + (!string.IsNullOrEmpty(entry.ChildVoice) ? $" — \"{entry.ChildVoice}\"" : ""),
                    Date = entry.ObservationDate,
                    Category = "Journey",
                    IsVideo = false
                });
            }
        }

        return mediaList.OrderByDescending(m => m.Date);
    }

}
