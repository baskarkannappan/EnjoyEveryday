using System;

namespace EnjoyEveryday.Domain.Entities;

public class OrganizationPlanHistory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? PreviousPlanId { get; set; }
    public string? PreviousPlanCode { get; set; }
    public string? PreviousPlanName { get; set; }
    public Guid? PreviousTariffId { get; set; }
    public string? PreviousTariffName { get; set; }
    public decimal? PreviousRate { get; set; }
    public string? PreviousCurrency { get; set; }
    public Guid NewPlanId { get; set; }
    public string NewPlanCode { get; set; } = string.Empty;
    public string NewPlanName { get; set; } = string.Empty;
    public Guid? NewTariffId { get; set; }
    public string? NewTariffName { get; set; }
    public decimal NewRate { get; set; }
    public string NewCurrency { get; set; } = "INR";
    public int MaxBranches { get; set; }
    public int MaxClassrooms { get; set; }
    public int MaxChildren { get; set; }
    public bool IncludesAi { get; set; }
    public string ChangeType { get; set; } = "Initial";
    public string? Reason { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? ChangedByEmail { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsCurrent => EffectiveTo == null;
}

public class OrganizationPlanChangeRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid CurrentPlanId { get; set; }
    public string CurrentPlanName { get; set; } = string.Empty;
    public Guid? CurrentTariffId { get; set; }
    public decimal CurrentRate { get; set; }
    public string CurrentCurrency { get; set; } = "INR";
    public Guid RequestedPlanId { get; set; }
    public string RequestedPlanName { get; set; } = string.Empty;
    public Guid? RequestedTariffId { get; set; }
    public string RequestType { get; set; } = "Upgrade";
    public string? OrgAdminNotes { get; set; }
    public Guid? ProposedPlanId { get; set; }
    public string? ProposedPlanName { get; set; }
    public Guid? ProposedTariffId { get; set; }
    public string? ProposedTariffName { get; set; }
    public decimal? ProposedRate { get; set; }
    public string? ProposedCurrency { get; set; }
    public DateTimeOffset? ProposedEffectiveDate { get; set; }
    public string? PlatformAdminNotes { get; set; }
    public string Status { get; set; } = PlanChangeRequestStatuses.PendingPlatformReview;
    public Guid? RequestedByUserId { get; set; }
    public string? RequestedByEmail { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByEmail { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? RespondedByUserId { get; set; }
    public string? RespondedByEmail { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public string? OrgResponseNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public static class PlanChangeRequestStatuses
{
    public const string PendingPlatformReview = "PendingPlatformReview";
    public const string PendingOrgAcceptance = "PendingOrgAcceptance";
    public const string Accepted = "Accepted";
    public const string DeclinedByOrg = "DeclinedByOrg";
    public const string RejectedByPlatform = "RejectedByPlatform";
    public const string Cancelled = "Cancelled";
}

public class OrganizationSubscriptionOverview
{
    public Guid OrganizationId { get; set; }
    public Guid TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public Guid? TariffId { get; set; }
    public string TariffName { get; set; } = "Monthly Standard";
    public string BillingFrequency { get; set; } = "Monthly";
    public decimal CurrentRate { get; set; }
    public string Currency { get; set; } = "INR";
    public Guid? PricingRegionId { get; set; }
    public string? PricingRegionName { get; set; }
    public int MaxBranches { get; set; }
    public int CurrentBranches { get; set; }
    public int MaxClassrooms { get; set; }
    public int CurrentClassrooms { get; set; }
    public int MaxChildren { get; set; }
    public int CurrentChildren { get; set; }
    public bool IncludesAi { get; set; }
    public DateTimeOffset? SubscribedSince { get; set; }
}

