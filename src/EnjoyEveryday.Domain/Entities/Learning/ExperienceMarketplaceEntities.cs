using System;

namespace EnjoyEveryday.Domain.Entities;

public static class HarvestRequestStatus
{
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string ChangesRequested = "ChangesRequested";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Withdrawn = "Withdrawn";
}

public static class PlatformPublicationStatus
{
    public const string Draft = "Draft";
    public const string InReview = "InReview";
    public const string Published = "Published";
    public const string Suspended = "Suspended";
    public const string Retired = "Retired";
}

public static class ExperiencePricingModel
{
    public const string Free = "Free";
    public const string IncludedInSubscription = "IncludedInSubscription";
    public const string OneTimePurchase = "OneTimePurchase";
    public const string TieredAddOn = "TieredAddOn";
}

public static class EntitlementStatus
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Revoked = "Revoked";
    public const string Expired = "Expired";
}

public class ExperienceLicense
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ExperienceId { get; set; }
    public string LicenseType { get; set; } = "CommercialRoyalty";
    public string GrantScope { get; set; } = "Curate, Reproduce, Distribute, Modify and Monetize";
    public Guid GrantedByUserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? TermsDocumentHash { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExperienceHarvestRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SourceExperienceId { get; set; }
    public int SourceVersionNumber { get; set; } = 1;
    public Guid? LicenseId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = HarvestRequestStatus.Submitted;
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public bool SafetySuitabilityVerified { get; set; }
    public bool LearningObjectivesVerified { get; set; }
    public bool InstructionsVerified { get; set; }
    public bool MaterialsVerified { get; set; }
    public bool MediaRightsVerified { get; set; }
    public bool CommercialRightsVerified { get; set; }

    // Navigation / Projection helpers
    public string? SourceExperienceTitle { get; set; }
    public string? OrganizationName { get; set; }
    public string? SubmitterEmail { get; set; }
}

public class PlatformExperience
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public int? AgeRangeMinMonths { get; set; }
    public int? AgeRangeMaxMonths { get; set; }
    public int? DurationMinutes { get; set; }
    public string? LearningObjectives { get; set; }
    public string? RequiredMaterials { get; set; }
    public string? Instructions { get; set; }
    public string? SafetyNotes { get; set; }
    public string DnaPayload { get; set; } = "{}";
    public string PricingModel { get; set; } = ExperiencePricingModel.IncludedInSubscription;
    public decimal PriceAmount { get; set; } = 0.00m;
    public string Currency { get; set; } = "INR";
    public string PublicationStatus { get; set; } = PlatformPublicationStatus.Draft;
    public string OwnershipType { get; set; } = "CuratedLicensed";
    public int CurrentVersion { get; set; } = 1;
    public Guid? LicenseId { get; set; }
    public Guid? SourceHarvestRequestId { get; set; }

    // Provenance (Restricted to Platform Admins)
    public Guid? SourceExperienceId { get; set; }
    public Guid? SourceOrganizationId { get; set; }

    public string? CreatorAttribution { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Derived display helpers
    public bool IsPurchasedOrEntitled { get; set; }
}

public class PlatformExperienceVersion
{
    public Guid Id { get; set; }
    public Guid PlatformExperienceId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DnaPayload { get; set; } = "{}";
    public string? ChangeSummary { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class MarketplaceOrder
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PlatformExperienceId { get; set; }
    public Guid PurchasedByUserId { get; set; }
    public string PricingModel { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentStatus { get; set; } = "Completed";
    public string TransactionReference { get; set; } = string.Empty;
    public DateTimeOffset PurchasedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
}

public class OrganizationEntitlement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PlatformExperienceId { get; set; }
    public Guid? OrderId { get; set; }
    public int AcquiredVersion { get; set; } = 1;
    public string EntitlementType { get; set; } = "OneTimePurchase";
    public string Status { get; set; } = EntitlementStatus.Active;
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Scope { get; set; } = "OrganizationWide";
}

public class ExperienceAuditEvent
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? PlatformExperienceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedByUserId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
