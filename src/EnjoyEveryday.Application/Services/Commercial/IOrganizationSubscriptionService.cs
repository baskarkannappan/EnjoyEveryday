using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public class AvailableSubscriptionPlanOption
{
    public Guid PlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxBranches { get; set; }
    public int MaxClassrooms { get; set; }
    public int MaxChildren { get; set; }
    public bool IncludesAi { get; set; }
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "INR";
    public List<RegionPlanTariff> RegionalTariffs { get; set; } = new();
}

public interface IOrganizationSubscriptionService
{
    Task<OrganizationSubscriptionOverview?> GetOverviewAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<OrganizationPlanHistory>> GetPlanHistoryAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<OrganizationPlanChangeRequest>> GetRequestsForOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<OrganizationPlanChangeRequest>> GetAllRequestsAsync(string? status = null, CancellationToken ct = default);
    Task<List<AvailableSubscriptionPlanOption>> GetAvailablePlansForOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    Task<OrganizationPlanChangeRequest> SubmitPlanChangeRequestAsync(
        Guid organizationId,
        Guid requestedPlanId,
        Guid? requestedTariffId,
        string? orgAdminNotes,
        Guid? requestedByUserId,
        string? requestedByEmail,
        CancellationToken ct = default);

    Task<bool> ProposePlanChangeForAcceptanceAsync(
        Guid requestId,
        Guid proposedPlanId,
        Guid? proposedTariffId,
        decimal? overrideRate,
        string? currency,
        DateTimeOffset effectiveDate,
        string? platformAdminNotes,
        Guid? reviewedByUserId,
        string? reviewedByEmail,
        CancellationToken ct = default);

    Task<bool> RejectPlanChangeRequestAsync(
        Guid requestId,
        string? platformAdminNotes,
        Guid? reviewedByUserId,
        string? reviewedByEmail,
        CancellationToken ct = default);

    Task<bool> RespondToProposalAsync(
        Guid requestId,
        bool accept,
        string? orgResponseNotes,
        Guid? respondedByUserId,
        string? respondedByEmail,
        CancellationToken ct = default);
}

