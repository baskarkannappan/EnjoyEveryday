using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class OrganizationSubscriptionService : IOrganizationSubscriptionService
{
    private static readonly Guid SproutPlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BloomPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CanopyPlanId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid DefaultGlobalRegionId = Guid.Parse("44444444-4444-4444-4444-444444444404");

    private readonly IOrganizationSubscriptionRepository _subscriptionRepo;
    private readonly IPlatformAdminRepository _platformAdminRepo;
    private readonly IEmailNotificationService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrganizationSubscriptionService> _logger;
    private readonly IPlatformBusinessEventService? _businessEventService;

    public OrganizationSubscriptionService(
        IOrganizationSubscriptionRepository subscriptionRepo,
        IPlatformAdminRepository platformAdminRepo,
        IEmailNotificationService emailService,
        IConfiguration configuration,
        ILogger<OrganizationSubscriptionService> logger,
        IPlatformBusinessEventService? businessEventService = null)
    {
        _subscriptionRepo = subscriptionRepo;
        _platformAdminRepo = platformAdminRepo;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _businessEventService = businessEventService;
    }

    public Task<OrganizationSubscriptionOverview?> GetOverviewAsync(Guid organizationId, CancellationToken ct = default)
        => _subscriptionRepo.GetOrganizationSubscriptionOverviewAsync(organizationId, ct);

    public Task<List<OrganizationPlanHistory>> GetPlanHistoryAsync(Guid organizationId, CancellationToken ct = default)
        => _subscriptionRepo.GetPlanHistoryAsync(organizationId, ct);

    public Task<List<OrganizationPlanChangeRequest>> GetRequestsForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => _subscriptionRepo.GetPlanChangeRequestsByOrgAsync(organizationId, ct);

    public Task<List<OrganizationPlanChangeRequest>> GetAllRequestsAsync(string? status = null, CancellationToken ct = default)
        => _subscriptionRepo.GetAllPlanChangeRequestsAsync(status, ct);

    public async Task<List<AvailableSubscriptionPlanOption>> GetAvailablePlansForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var overview = await _subscriptionRepo.GetOrganizationSubscriptionOverviewAsync(organizationId, ct);
        var regionId = overview?.PricingRegionId ?? DefaultGlobalRegionId;
        var currency = overview?.Currency ?? "INR";

        List<RegionPlanTariff> tariffs = new();
        try
        {
            tariffs = await _platformAdminRepo.GetRegionPlanTariffsAsync(regionId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load regional tariffs for region {RegionId}", regionId);
        }

        var options = new List<AvailableSubscriptionPlanOption>
        {
            new()
            {
                PlanId = SproutPlanId,
                PlanCode = "PLAN-SPROUT",
                PlanName = "Sprout (Starter)",
                Description = "Ideal for single-site boutique nurseries and small early-learning centers.",
                MaxBranches = 1,
                MaxClassrooms = 3,
                MaxChildren = 35,
                IncludesAi = false,
                BasePrice = 1499m,
                Currency = currency,
                RegionalTariffs = tariffs.Where(t => t.PlanId == SproutPlanId && t.IsActive).OrderBy(t => t.Price).ToList()
            },
            new()
            {
                PlanId = BloomPlanId,
                PlanCode = "PLAN-BLOOM",
                PlanName = "Bloom (Growth)",
                Description = "Built for growing preschools with multi-classroom orchestration and AI insights.",
                MaxBranches = 2,
                MaxClassrooms = 8,
                MaxChildren = 90,
                IncludesAi = true,
                BasePrice = 2999m,
                Currency = currency,
                RegionalTariffs = tariffs.Where(t => t.PlanId == BloomPlanId && t.IsActive).OrderBy(t => t.Price).ToList()
            },
            new()
            {
                PlanId = CanopyPlanId,
                PlanCode = "PLAN-CANOPY",
                PlanName = "Canopy (Multi-Branch)",
                Description = "Enterprise-grade multi-campus management with full AI curriculum & operations.",
                MaxBranches = 6,
                MaxClassrooms = 24,
                MaxChildren = 300,
                IncludesAi = true,
                BasePrice = 6999m,
                Currency = currency,
                RegionalTariffs = tariffs.Where(t => t.PlanId == CanopyPlanId && t.IsActive).OrderBy(t => t.Price).ToList()
            }
        };

        return options;
    }

    public async Task<OrganizationPlanChangeRequest> SubmitPlanChangeRequestAsync(
        Guid organizationId,
        Guid requestedPlanId,
        Guid? requestedTariffId,
        string? orgAdminNotes,
        Guid? requestedByUserId,
        string? requestedByEmail,
        CancellationToken ct = default)
    {
        var overview = await _subscriptionRepo.GetOrganizationSubscriptionOverviewAsync(organizationId, ct)
            ?? throw new InvalidOperationException("Organization subscription details could not be found.");

        var existingRequests = await _subscriptionRepo.GetPlanChangeRequestsByOrgAsync(organizationId, ct);
        if (existingRequests.Any(r =>
                r.Status == PlanChangeRequestStatuses.PendingPlatformReview ||
                r.Status == PlanChangeRequestStatuses.PendingOrgAcceptance))
        {
            throw new InvalidOperationException("There is already an active plan change request in progress for your organization.");
        }

        var plans = await GetAvailablePlansForOrganizationAsync(organizationId, ct);
        var targetPlan = plans.FirstOrDefault(p => p.PlanId == requestedPlanId)
            ?? throw new InvalidOperationException("Selected subscription plan was not found.");

        var currentTier = GetPlanTierOrder(overview.PlanId);
        var requestedTier = GetPlanTierOrder(requestedPlanId);
        var requestType = requestedTier > currentTier
            ? "Upgrade"
            : requestedTier < currentTier
                ? "Downgrade"
                : "Change";

        var req = new OrganizationPlanChangeRequest
        {
            TenantId = overview.TenantId,
            OrganizationId = overview.OrganizationId,
            OrganizationName = overview.OrganizationName,
            CurrentPlanId = overview.PlanId,
            CurrentPlanName = overview.PlanName,
            CurrentTariffId = overview.TariffId,
            CurrentRate = overview.CurrentRate,
            CurrentCurrency = overview.Currency,
            RequestedPlanId = targetPlan.PlanId,
            RequestedPlanName = targetPlan.PlanName,
            RequestedTariffId = requestedTariffId,
            RequestType = requestType,
            OrgAdminNotes = orgAdminNotes?.Trim(),
            Status = PlanChangeRequestStatuses.PendingPlatformReview,
            RequestedByUserId = requestedByUserId,
            RequestedByEmail = !string.IsNullOrWhiteSpace(requestedByEmail) ? requestedByEmail : overview.AdminEmail
        };

        var created = await _subscriptionRepo.CreatePlanChangeRequestAsync(req, ct);

        // Notify Platform Admin via Email (CC Org Admin)
        var platformAdminEmail = GetPlatformAdminEmail();
        var adminPortalUrl = $"{GetAdminWebBaseUrl()}/organizations";
        try
        {
            await _emailService.SendPlanChangeRequestedAsync(
                platformAdminEmail: platformAdminEmail,
                organizationName: overview.OrganizationName,
                orgAdminEmail: req.RequestedByEmail ?? overview.AdminEmail,
                currentPlanName: overview.PlanName,
                requestedPlanName: targetPlan.PlanName,
                requestType: requestType,
                notes: orgAdminNotes,
                adminPortalUrl: adminPortalUrl,
                tenantId: overview.TenantId,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispatch plan change request email for Org {OrgId}", organizationId);
        }

        // Notify Platform Admin Notification Bell
        if (_businessEventService != null)
        {
            try
            {
                await _businessEventService.PublishEventAsync(
                    new PublishBusinessEventRequest
                    {
                        EventType = "PlanChangeRequested",
                        TenantId = overview.TenantId,
                        OrganizationId = overview.OrganizationId,
                        InitiatedByUserId = requestedByUserId,
                        TransactionCategory = "Subscription",
                        Title = $"Plan {requestType} Request: {overview.OrganizationName}",
                        Description = $"{overview.OrganizationName} requested to {requestType.ToLowerInvariant()} from {overview.PlanName} to {targetPlan.PlanName}.",
                        Status = "PendingPlatformReview",
                        ActionUrl = "/plan-requests",
                        SendPlatformAdminEmail = false,
                        Metadata = new { requestId = created.Id, requestedPlanId = targetPlan.PlanId }
                    },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish PlanChangeRequested business event for Org {OrgId}", organizationId);
            }
        }

        return created;
    }

    public async Task<bool> ProposePlanChangeForAcceptanceAsync(
        Guid requestId,
        Guid proposedPlanId,
        Guid? proposedTariffId,
        decimal? overrideRate,
        string? currency,
        DateTimeOffset effectiveDate,
        string? platformAdminNotes,
        Guid? reviewedByUserId,
        string? reviewedByEmail,
        CancellationToken ct = default)
    {
        var req = await _subscriptionRepo.GetPlanChangeRequestByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("Plan change request not found.");

        var overview = await _subscriptionRepo.GetOrganizationSubscriptionOverviewAsync(req.OrganizationId, ct)
            ?? throw new InvalidOperationException("Organization not found.");

        var plans = await GetAvailablePlansForOrganizationAsync(req.OrganizationId, ct);
        var proposedPlan = plans.FirstOrDefault(p => p.PlanId == proposedPlanId)
            ?? throw new InvalidOperationException("Proposed subscription plan not found.");

        var tariff = proposedTariffId.HasValue
            ? proposedPlan.RegionalTariffs.FirstOrDefault(t => t.Id == proposedTariffId.Value)
            : proposedPlan.RegionalTariffs.FirstOrDefault();

        var resolvedCurrency = !string.IsNullOrWhiteSpace(currency)
            ? currency.Trim().ToUpperInvariant()
            : tariff?.Currency ?? (!string.IsNullOrWhiteSpace(overview.Currency) ? overview.Currency : "INR");

        var resolvedRate = overrideRate ?? tariff?.Price ?? proposedPlan.BasePrice;
        var tariffName = tariff?.Name ?? "Monthly Standard";
        var effectiveDateIst = effectiveDate.ToOffset(TimeSpan.FromMinutes(330));

        var updated = await _subscriptionRepo.UpdatePlanChangeRequestProposalAsync(
            requestId: requestId,
            proposedPlanId: proposedPlan.PlanId,
            proposedPlanName: proposedPlan.PlanName,
            proposedTariffId: tariff?.Id ?? proposedTariffId,
            proposedTariffName: tariffName,
            proposedRate: resolvedRate,
            proposedCurrency: resolvedCurrency,
            proposedEffectiveDate: effectiveDate,
            platformAdminNotes: platformAdminNotes?.Trim(),
            reviewedByUserId: reviewedByUserId,
            reviewedByEmail: reviewedByEmail ?? GetPlatformAdminEmail(),
            ct: ct);

        if (!updated) return false;

        var recipientEmail = !string.IsNullOrWhiteSpace(overview.AdminEmail)
            ? overview.AdminEmail
            : req.RequestedByEmail ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(recipientEmail))
        {
            var subscriptionUrl = $"{GetDaycareWebBaseUrl()}/management/subscription";
            try
            {
                await _emailService.SendPlanChangeProposalForAcceptanceAsync(
                    orgAdminEmail: recipientEmail,
                    organizationName: overview.OrganizationName,
                    currentPlanName: overview.PlanName,
                    proposedPlanName: proposedPlan.PlanName,
                    proposedTariffName: tariffName,
                    formattedRate: $"{resolvedCurrency} {resolvedRate:N0}",
                    effectiveDateText: $"{effectiveDateIst:MMM dd, yyyy} (IST)",
                    platformNotes: platformAdminNotes,
                    subscriptionPortalUrl: subscriptionUrl,
                    tenantId: overview.TenantId,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send plan proposal acceptance email for request {RequestId}", requestId);
            }
        }

        return true;
    }

    public async Task<bool> RejectPlanChangeRequestAsync(
        Guid requestId,
        string? platformAdminNotes,
        Guid? reviewedByUserId,
        string? reviewedByEmail,
        CancellationToken ct = default)
    {
        var req = await _subscriptionRepo.GetPlanChangeRequestByIdAsync(requestId, ct);
        if (req == null) return false;

        var rejected = await _subscriptionRepo.RejectPlanChangeRequestByPlatformAsync(
            requestId,
            platformAdminNotes?.Trim(),
            reviewedByUserId,
            reviewedByEmail ?? GetPlatformAdminEmail(),
            ct);

        if (!rejected) return false;

        var overview = await _subscriptionRepo.GetOrganizationSubscriptionOverviewAsync(req.OrganizationId, ct);
        var recipientEmail = overview?.AdminEmail ?? req.RequestedByEmail ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(recipientEmail))
        {
            try
            {
                await _emailService.SendPlanChangeDecisionConfirmationAsync(
                    orgAdminEmail: recipientEmail,
                    platformAdminEmail: GetPlatformAdminEmail(),
                    organizationName: req.OrganizationName,
                    planName: req.RequestedPlanName,
                    accepted: false,
                    decisionNotes: platformAdminNotes ?? "Request declined by Platform Administration.",
                    subscriptionPortalUrl: $"{GetDaycareWebBaseUrl()}/management/subscription",
                    tenantId: req.TenantId,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send plan rejection email for request {RequestId}", requestId);
            }
        }

        return true;
    }

    public async Task<bool> RespondToProposalAsync(
        Guid requestId,
        bool accept,
        string? orgResponseNotes,
        Guid? respondedByUserId,
        string? respondedByEmail,
        CancellationToken ct = default)
    {
        var req = await _subscriptionRepo.GetPlanChangeRequestByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("Plan change proposal not found.");

        if (req.Status != PlanChangeRequestStatuses.PendingOrgAcceptance)
        {
            throw new InvalidOperationException("This plan proposal is no longer awaiting acceptance.");
        }

        var overview = await _subscriptionRepo.GetOrganizationSubscriptionOverviewAsync(req.OrganizationId, ct)
            ?? throw new InvalidOperationException("Organization not found.");

        var newStatus = accept ? PlanChangeRequestStatuses.Accepted : PlanChangeRequestStatuses.DeclinedByOrg;
        var updated = await _subscriptionRepo.UpdateOrgResponseAsync(
            requestId,
            newStatus,
            orgResponseNotes?.Trim(),
            respondedByUserId,
            respondedByEmail ?? overview.AdminEmail,
            ct);

        if (!updated) return false;

        var targetPlanId = req.ProposedPlanId ?? req.RequestedPlanId;
        var targetPlanName = req.ProposedPlanName ?? req.RequestedPlanName;

        if (accept)
        {
            var regionId = overview.PricingRegionId ?? DefaultGlobalRegionId;
            var currency = req.ProposedCurrency ?? overview.Currency;
            var reason = string.IsNullOrWhiteSpace(req.PlatformAdminNotes)
                ? $"Org Admin accepted {req.RequestType.ToLowerInvariant()} to {targetPlanName}"
                : $"Org Admin accepted {req.RequestType.ToLowerInvariant()}: {req.PlatformAdminNotes}";

            await _subscriptionRepo.ApplyApprovedPlanChangeAsync(
                organizationId: req.OrganizationId,
                regionId: regionId,
                planId: targetPlanId,
                tariffId: req.ProposedTariffId ?? req.RequestedTariffId,
                currency: currency,
                changeType: req.RequestType,
                reason: reason,
                changedByUserId: respondedByUserId,
                changedByEmail: respondedByEmail ?? overview.AdminEmail,
                ct: ct);
        }

        // Send confirmation email to Org Admin & CC Platform Admin
        var orgEmail = !string.IsNullOrWhiteSpace(overview.AdminEmail)
            ? overview.AdminEmail
            : respondedByEmail ?? req.RequestedByEmail ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(orgEmail))
        {
            try
            {
                await _emailService.SendPlanChangeDecisionConfirmationAsync(
                    orgAdminEmail: orgEmail,
                    platformAdminEmail: GetPlatformAdminEmail(),
                    organizationName: overview.OrganizationName,
                    planName: targetPlanName,
                    accepted: accept,
                    decisionNotes: orgResponseNotes,
                    subscriptionPortalUrl: $"{GetDaycareWebBaseUrl()}/management/subscription",
                    tenantId: overview.TenantId,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send plan decision confirmation email for request {RequestId}", requestId);
            }
        }

        if (_businessEventService != null)
        {
            try
            {
                await _businessEventService.PublishEventAsync(
                    new PublishBusinessEventRequest
                    {
                        EventType = accept ? "PlanChangeAccepted" : "PlanChangeDeclined",
                        TenantId = overview.TenantId,
                        OrganizationId = overview.OrganizationId,
                        InitiatedByUserId = respondedByUserId,
                        TransactionCategory = "Subscription",
                        Title = accept
                            ? $"Plan Change Accepted: {overview.OrganizationName}"
                            : $"Plan Change Declined: {overview.OrganizationName}",
                        Description = $"{overview.OrganizationName} {(accept ? "accepted and activated" : "declined")} the {targetPlanName} plan proposal.",
                        Status = accept ? "Completed" : "Declined",
                        ActionUrl = "/plan-requests",
                        SendPlatformAdminEmail = false,
                        Metadata = new { requestId = req.Id, planName = targetPlanName, accepted = accept }
                    },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish plan decision event for request {RequestId}", requestId);
            }
        }

        return true;
    }

    private static int GetPlanTierOrder(Guid planId)
    {
        if (planId == SproutPlanId) return 1;
        if (planId == BloomPlanId) return 2;
        if (planId == CanopyPlanId) return 3;
        return 2;
    }

    private string GetPlatformAdminEmail()
        => _configuration["PlatformAdmin:NotificationEmail"]
           ?? _configuration["AzureCommunicationServices:SenderAddress"]
           ?? "platform-admin@enjoyeveryday.com";

    private string GetDaycareWebBaseUrl()
        => (_configuration["Portals:DaycareWebUrl"] ?? "http://localhost:7003").TrimEnd('/');

    private string GetAdminWebBaseUrl()
        => (_configuration["Portals:AdminWebUrl"] ?? "http://localhost:7002").TrimEnd('/');
}

