using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IOrganizationSubscriptionRepository
{
    Task<OrganizationSubscriptionOverview?> GetOrganizationSubscriptionOverviewAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<OrganizationPlanHistory>> GetPlanHistoryAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<OrganizationPlanChangeRequest>> GetPlanChangeRequestsByOrgAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<OrganizationPlanChangeRequest>> GetAllPlanChangeRequestsAsync(string? status = null, CancellationToken ct = default);
    Task<OrganizationPlanChangeRequest?> GetPlanChangeRequestByIdAsync(Guid requestId, CancellationToken ct = default);
    Task<OrganizationPlanChangeRequest> CreatePlanChangeRequestAsync(OrganizationPlanChangeRequest request, CancellationToken ct = default);
    Task<bool> UpdatePlanChangeRequestProposalAsync(
        Guid requestId,
        Guid proposedPlanId,
        string proposedPlanName,
        Guid? proposedTariffId,
        string? proposedTariffName,
        decimal proposedRate,
        string proposedCurrency,
        DateTimeOffset proposedEffectiveDate,
        string? platformAdminNotes,
        Guid? reviewedByUserId,
        string? reviewedByEmail,
        CancellationToken ct = default);
    Task<bool> RejectPlanChangeRequestByPlatformAsync(
        Guid requestId,
        string? platformAdminNotes,
        Guid? reviewedByUserId,
        string? reviewedByEmail,
        CancellationToken ct = default);
    Task<bool> UpdateOrgResponseAsync(
        Guid requestId,
        string status,
        string? orgResponseNotes,
        Guid? respondedByUserId,
        string? respondedByEmail,
        CancellationToken ct = default);
    Task ApplyApprovedPlanChangeAsync(
        Guid organizationId,
        Guid regionId,
        Guid planId,
        Guid? tariffId,
        string currency,
        string changeType,
        string reason,
        Guid? changedByUserId,
        string? changedByEmail,
        CancellationToken ct = default);
}

