using System;

namespace EnjoyEveryday.Domain.Entities;

public static class ConsentWorkflowStatus
{
    public const string Draft = "Draft";
    public const string PendingAdminReview = "PendingAdminReview";
    public const string ApprovedToSend = "ApprovedToSend";
    public const string SentToParent = "SentToParent";
    public const string Opened = "Opened";
    public const string PartiallySigned = "PartiallySigned";
    public const string Signed = "Signed";
    public const string Declined = "Declined";
    public const string ChangesRequested = "ChangesRequested";
    public const string Expired = "Expired";
    public const string Withdrawn = "Withdrawn";
    public const string Superseded = "Superseded";
}

public static class ConsentDocumentType
{
    public const string GeneralDaycareAgreement = "GeneralDaycareAgreement";
    public const string EmergencyMedical = "EmergencyMedical";
    public const string PhotoVideoConsent = "PhotoVideoConsent";
    public const string AuthorizedPickup = "AuthorizedPickup";
    public const string FieldTrip = "FieldTrip";
    public const string Custom = "Custom";
}

public static class ConsentChoiceValue
{
    public const string Granted = "Granted";
    public const string Declined = "Declined";
    public const string Acknowledged = "Acknowledged";
    public const string NotApplicable = "NotApplicable";
}

public class ConsentTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DocumentType { get; set; } = ConsentDocumentType.GeneralDaycareAgreement;
    public string Version { get; set; } = "1.0";
    public string TemplateContent { get; set; } = string.Empty;
    public bool RequiredForEnrollment { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ChildConsentDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid ChildId { get; set; }
    public Guid ConsentTemplateId { get; set; }
    public string TemplateVersion { get; set; } = "1.0";
    public int DocumentVersion { get; set; } = 1;
    public string DocumentTitle { get; set; } = string.Empty;
    public string DocumentSnapshot { get; set; } = string.Empty;
    public string Status { get; set; } = ConsentWorkflowStatus.Draft;
    public DateTime? SigningDeadline { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? AdminReviewedBy { get; set; }
    public DateTime? AdminReviewedAt { get; set; }
    public string? AdminReviewNotes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public string? SignedDocumentStorageKey { get; set; }
    public string? SignedDocumentChecksum { get; set; }
    public Guid? SupersedesDocumentId { get; set; }
    public int RowVersion { get; set; } = 1;

    // Navigation / Display helpers
    public string? ChildName { get; set; }
    public string? BranchName { get; set; }
    public string? ClassroomName { get; set; }
    public List<ConsentSigner> Signers { get; set; } = new();
    public List<ConsentChoice> Choices { get; set; } = new();
    public List<ConsentAuditEvent> AuditEvents { get; set; } = new();
}

public class ConsentSigner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ConsentDocumentId { get; set; }
    public Guid? FamilyRelationshipId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? IntendedSignerUserId { get; set; }
    public string SignerNameSnapshot { get; set; } = string.Empty;
    public string SignerEmailSnapshot { get; set; } = string.Empty;
    public string SignerRelationship { get; set; } = "Parent/Guardian";
    public bool IsRequired { get; set; } = true;
    public string SigningStatus { get; set; } = "Pending"; // Pending, Opened, Signed, Declined, Expired, Revoked
    public string? TokenHash { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public int InvitationSentCount { get; set; }
    public DateTime? LastInvitationSentAt { get; set; }
    public DateTime? LastOpenedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureLegalName { get; set; }
    public string? SignatureIntentText { get; set; }
    public string? SignatureIpAddress { get; set; }
    public string? SignatureUserAgent { get; set; }
}

public class ConsentChoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ConsentDocumentId { get; set; }
    public Guid? ConsentSignerId { get; set; }
    public string SectionKey { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string Choice { get; set; } = ConsentChoiceValue.Acknowledged;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class ConsentAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid ConsentDocumentId { get; set; }
    public string ActorType { get; set; } = "Admin"; // Admin, Parent, System
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string EventType { get; set; } = "Created"; // Created, SubmittedForReview, Approved, Rejected, Sent, EmailDelivered, LinkOpened, SigningStarted, Signed, Declined, Expired, ReminderSent, Withdrawn, Superseded, Downloaded
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? SafeMetadata { get; set; }
}
