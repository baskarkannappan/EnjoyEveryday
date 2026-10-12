using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public static class ParentCommunicationTypes
{
    public const string DailyUpdate = "DailyUpdate";
    public const string WeeklySummary = "WeeklySummary";
    public const string ActivityUpdate = "ActivityUpdate";
    public const string MilestoneCelebration = "MilestoneCelebration";
    public const string EventFollowUp = "EventFollowUp";
}

public static class ParentCommunicationStatus
{
    public const string Draft = "Draft";
    public const string PendingReview = "PendingReview";
    public const string Approved = "Approved";
    public const string Sending = "Sending";
    public const string Sent = "Sent";
    public const string DeliveryFailed = "DeliveryFailed";
    public const string Cancelled = "Cancelled";
}

public class ParentCommunication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid ChildId { get; set; }

    public Guid? RecipientPersonId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientRelationship { get; set; }

    public string CommunicationType { get; set; } = ParentCommunicationTypes.DailyUpdate;
    public DateOnly ReportingDateStart { get; set; }
    public DateOnly ReportingDateEnd { get; set; }

    public string Title { get; set; } = string.Empty;
    public string ContentDraft { get; set; } = string.Empty;
    public string ContentFinal { get; set; } = string.Empty;
    public string? FamilyConversationStarter { get; set; }
    public string? EvidenceSummaryJson { get; set; }

    public bool ConsentVerified { get; set; } = true;
    public string? ConsentNotes { get; set; }

    public string Status { get; set; } = ParentCommunicationStatus.Draft;
    public Guid TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }

    public string DeliveryChannel { get; set; } = "PortalAndEmail";
    public string DeliveryStatus { get; set; } = "Pending";
    public string? DeliveryError { get; set; }
    public string? IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Helper navigation / display properties
    public string? ChildName { get; set; }
    public string? ClassroomName { get; set; }
}

public class ParentCommunicationRecipientInfo
{
    public Guid PersonId { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? MobilePhone { get; set; }
    public bool CanReceiveInformation { get; set; } = true;
    public bool ReceivesDailyUpdates { get; set; } = true;
    public bool HasPortalAccess { get; set; }
    public bool PhotoConsentGranted { get; set; } = true;
}

public class ParentCommunicationEvidencePackage
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = string.Empty;
    public string ReportingPeriod { get; set; } = string.Empty;
    public string AttendanceSummary { get; set; } = string.Empty;
    public List<string> CompletedExperiences { get; set; } = new();
    public List<string> ApprovedMoments { get; set; } = new();
    public List<string> CareHighlights { get; set; } = new();
    public List<string> MilestoneHighlights { get; set; } = new();
    public List<string> RelevantEvents { get; set; } = new();
    public string? TeacherNotes { get; set; }
}

public class GenerateParentCommunicationInput
{
    public Guid ChildId { get; set; }
    public Guid? ClassroomId { get; set; }
    public string CommunicationType { get; set; } = ParentCommunicationTypes.DailyUpdate;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? RecipientPersonId { get; set; }
    public string? TeacherNotes { get; set; }
}

public class UpdateParentCommunicationDraftInput
{
    public Guid CommunicationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentFinal { get; set; } = string.Empty;
    public string? FamilyConversationStarter { get; set; }
    public string? TeacherNotes { get; set; }
}

public class SendParentCommunicationInput
{
    public Guid CommunicationId { get; set; }
    public string? DeliveryChannel { get; set; } = "PortalAndEmail";
}
