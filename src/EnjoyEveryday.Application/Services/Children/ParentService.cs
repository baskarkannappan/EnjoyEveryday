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
    public bool HasCareUpdatesToday { get; set; }
    public int PendingConsentCount { get; set; }
    public int UnpaidInvoicesCount { get; set; }
    public int UnreadMessagesCount { get; set; }
    public string AttendanceStatusLabel { get; set; } = "Not checked in today";
}

public class ParentMediaItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public string Category { get; set; } = "Memories"; // Memories, Celebration, Journey, Activity
    public string? ChildName { get; set; }
    public bool IsVideo { get; set; }
}

public class ParentChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public bool IsFromParent { get; set; }
    public string DeliveryStatus { get; set; } = "Sent";
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
    Task<IEnumerable<ParentMediaItem>> GetChildMemoriesAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParentChatMessage>> GetChildMessagesAsync(Guid childId, int take = 50, CancellationToken cancellationToken = default);
    Task<ParentChatMessage> SendParentMessageAsync(Guid childId, string messageText, CancellationToken cancellationToken = default);
    Task ReportAbsenceAsync(Guid childId, DateTime startDate, DateTime endDate, string reason, string? notes, CancellationToken cancellationToken = default);
    Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetChildInvoicesAsync(Guid childId, CancellationToken cancellationToken = default);
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
    private readonly IParentCommunicationRepository? _communicationRepository;
    private readonly ISecurePickupService? _pickupService;
    private readonly IBillingService? _billingService;
    private readonly IConsentService? _consentService;
    private readonly IRealtimeNotificationService? _realtimeNotificationService;

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
        IRealtimeNotificationService? realtimeNotificationService = null)
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
                var effectiveTenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;
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
            HasCareUpdatesToday = hasCareUpdates,
            AttendanceStatusLabel = attendanceStatus,
            UnreadMessagesCount = unreadMessages,
            PendingConsentCount = pendingConsent,
            UnpaidInvoicesCount = unpaidInvoices
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

    public async Task<IEnumerable<ParentMediaItem>> GetChildMemoriesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var mediaList = new List<ParentMediaItem>();

        // 1. Photos from approved classroom events & celebrations
        var events = await _parentRepository.GetApprovedEventsForChildAsync(_tenantContext.TenantId, childId, cancellationToken);
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
        var journeyEntries = await _parentRepository.GetApprovedJourneyEntriesAsync(_tenantContext.TenantId, childId, cancellationToken);
        foreach (var entry in journeyEntries)
        {
            if (!string.IsNullOrEmpty(entry.Observation))
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

    public async Task<IEnumerable<ParentChatMessage>> GetChildMessagesAsync(Guid childId, int take = 50, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var messages = new List<ParentChatMessage>();

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId);
        var effectiveTenantId = (child != null && child.TenantId != Guid.Empty) ? child.TenantId : _tenantContext.TenantId;

        if (_communicationRepository != null)
        {
            var comms = await _communicationRepository.GetByChildIdAsync(effectiveTenantId, childId, cancellationToken);
            foreach (var c in comms)
            {
                var isFromParent = (c.TeacherId == Guid.Empty);
                var content = !string.IsNullOrWhiteSpace(c.ContentFinal) ? c.ContentFinal : c.ContentDraft;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    messages.Add(new ParentChatMessage
                    {
                        Id = c.Id,
                        ChildId = c.ChildId,
                        SenderName = isFromParent ? "You" : (!string.IsNullOrWhiteSpace(c.TeacherName) ? c.TeacherName : $"{child?.ClassroomName ?? "Classroom"} Educators"),
                        Content = content,
                        Timestamp = c.SentAt ?? c.CreatedAt,
                        IsFromParent = isFromParent,
                        DeliveryStatus = c.DeliveryStatus ?? "Delivered"
                    });
                }
            }
        }

        // If no prior message history, seed an initial friendly educator check-in
        if (messages.Count == 0)
        {
            var childName = child?.FirstName ?? "Your child";
            var classroomName = child?.ClassroomName ?? "Classroom";

            messages.Add(new ParentChatMessage
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                SenderName = $"{classroomName} Educators",
                Content = $"Hello! Welcome to the {classroomName} communication channel. Feel free to message us here about {childName}'s day or questions.",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-3),
                IsFromParent = false,
                DeliveryStatus = "Read"
            });
        }

        return messages.OrderBy(m => m.Timestamp).TakeLast(take);
    }

    public async Task<ParentChatMessage> SendParentMessageAsync(Guid childId, string messageText, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var sanitized = messageText?.Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Message cannot be empty.", nameof(messageText));

        if (sanitized.Length > 2000)
            sanitized = sanitized.Substring(0, 2000);

        var person = await _parentRepository.GetPersonByUserIdAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var parentName = person != null ? $"{person.FirstName} {person.LastName}".Trim() : "Parent";

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");

        var messageId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        if (_communicationRepository != null)
        {
            var comm = new ParentCommunication
            {
                Id = messageId,
                TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
                OrganizationId = child.OrganizationId ?? Guid.Empty,
                ClassroomId = child.ClassroomId,
                ChildId = childId,
                RecipientUserId = _userContext.UserId,
                RecipientPersonId = person?.Id,
                RecipientName = parentName,
                RecipientRelationship = child.Relationship,
                CommunicationType = "ParentMessage",
                ReportingDateStart = DateOnly.FromDateTime(DateTime.Today),
                ReportingDateEnd = DateOnly.FromDateTime(DateTime.Today),
                Title = $"Message from {parentName}",
                ContentDraft = sanitized,
                ContentFinal = sanitized,
                Status = ParentCommunicationStatus.Sent,
                TeacherId = Guid.Empty,
                TeacherName = $"{child.ClassroomName ?? "Classroom"} Educators",
                SentAt = now,
                DeliveryChannel = "Portal",
                DeliveryStatus = "Delivered",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _communicationRepository.CreateAsync(comm, cancellationToken);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentMessageSent",
            EntityType = "ParentCommunication",
            EntityId = messageId,
            Details = new { ChildId = childId, TextLength = sanitized.Length }
        }, cancellationToken);

        if (_realtimeNotificationService != null)
        {
            _ = _realtimeNotificationService.NotifyParentMessageSentAsync(new ParentMessageNotification
            {
                MessageId = messageId,
                TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
                OrganizationId = child.OrganizationId ?? Guid.Empty,
                BranchId = child.BranchId,
                ClassroomId = child.ClassroomId,
                ChildId = childId,
                ChildName = $"{child.FirstName} {child.LastName}".Trim(),
                ClassroomName = child.ClassroomName ?? "Classroom",
                SenderName = parentName,
                Content = sanitized,
                Timestamp = now,
                IsFromParent = true
            }, cancellationToken);
        }

        return new ParentChatMessage
        {
            Id = messageId,
            ChildId = childId,
            SenderName = "You",
            Content = sanitized,
            Timestamp = now,
            IsFromParent = true,
            DeliveryStatus = "Delivered"
        };
    }

    public async Task ReportAbsenceAsync(Guid childId, DateTime startDate, DateTime endDate, string reason, string? notes, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");

        var person = await _parentRepository.GetPersonByUserIdAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var parentName = person != null ? $"{person.FirstName} {person.LastName}".Trim() : (_userContext.Email ?? "Parent");

        var messageId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var absenceDetails = $"Absence Notice: {startDate:d MMM yyyy} to {endDate:d MMM yyyy}. Reason: {reason}." + (!string.IsNullOrWhiteSpace(notes) ? $" Notes: {notes}" : "");

        if (_communicationRepository != null)
        {
            var comm = new ParentCommunication
            {
                Id = messageId,
                TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
                OrganizationId = child.OrganizationId ?? Guid.Empty,
                ClassroomId = child.ClassroomId,
                ChildId = childId,
                RecipientUserId = _userContext.UserId,
                RecipientPersonId = person?.Id,
                RecipientName = parentName,
                RecipientRelationship = child.Relationship,
                CommunicationType = "AbsenceNotice",
                ReportingDateStart = DateOnly.FromDateTime(startDate),
                ReportingDateEnd = DateOnly.FromDateTime(endDate),
                Title = $"Absence reported for {child.FirstName}",
                ContentDraft = absenceDetails,
                ContentFinal = absenceDetails,
                Status = ParentCommunicationStatus.PendingReview,
                TeacherId = Guid.Empty,
                TeacherName = $"{child.ClassroomName ?? "Classroom"} Educators",
                SentAt = now,
                DeliveryChannel = "Portal",
                DeliveryStatus = "Delivered",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _communicationRepository.CreateAsync(comm, cancellationToken);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentReportedAbsence",
            EntityType = "ChildAttendanceAbsenceNotice",
            EntityId = messageId,
            Details = new { ChildId = childId, StartDate = startDate, EndDate = endDate, Reason = reason }
        }, cancellationToken);
    }

    public async Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        if (_pickupService == null) return null;
        return await _pickupService.GetChildPickupSecurityProfileAsync(childId, null, cancellationToken);
    }

    public async Task<List<Invoice>> GetChildInvoicesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        if (_billingService == null) return new List<Invoice>();
        var allInvoices = await _billingService.GetAllInvoicesAsync(ct: cancellationToken);
        return allInvoices;
    }
}
