using EnjoyEveryday.Domain.Constants;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ChildDailyTimelineItem
{
    public DateTime EventTime { get; set; }
    public string TimeLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Attendance, Experience, Moment, DailyCare
    public string? Status { get; set; }
    public bool IsCompleted { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Icon { get; set; }
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
    public ExperienceHighlightDto? TodayExperienceHighlight { get; set; }
    public bool HasCareUpdatesToday { get; set; }
    public int PendingConsentCount { get; set; }
    public int UnpaidInvoicesCount { get; set; }
    public int UnreadMessagesCount { get; set; }
    public string AttendanceStatusLabel { get; set; } = "Not checked in today";
}

public class ExperienceHighlightDto
{
    public Guid ExperienceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MagicMoment { get; set; } = string.Empty;
    public string BedtimeConversationStarter { get; set; } = string.Empty;
    public string TryAtHomeExtension { get; set; } = string.Empty;
    public List<string> Materials { get; set; } = new();
    public List<string> DevelopmentalGoals { get; set; } = new();
    public string? TimeLabel { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsCompleted { get; set; }
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
    public bool IsAgentDraft { get; set; }
    public Guid? AgentTokenId { get; set; }
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
    Task<AuthorizedPickupDto> SaveAuthorizedPickupAsync(Guid childId, AuthorizedPickupDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAuthorizedPickupAsync(Guid childId, Guid pickupId, CancellationToken cancellationToken = default);
    Task<bool> IsPrivacyAndTrustApprovedAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetPrivacyAndTrustApprovalDateAsync(Guid childId, CancellationToken cancellationToken = default);
    Task ApprovePrivacyAndTrustAsync(Guid childId, string approvedByName, CancellationToken cancellationToken = default);
    Task MarkMessagesAsReadAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetChildInvoicesAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<ParentPaymentOrderDto> CreateInvoicePaymentOrderAsync(Guid childId, Guid invoiceId, CancellationToken ct = default);
    Task<ParentPaymentResultDto> VerifyAndApplyPaymentAsync(Guid childId, Guid invoiceId, string orderId, string paymentId, string signature, CancellationToken ct = default);
}

public class ParentPaymentOrderDto
{
    public string KeyId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public string InvoiceNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string PrefillName { get; set; } = string.Empty;
    public string PrefillEmail { get; set; } = string.Empty;
    public string PrefillContact { get; set; } = string.Empty;
}

public class ParentPaymentResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PaymentId { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal NewBalanceDue { get; set; }
    public string NewStatus { get; set; } = string.Empty;
}

