using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories.Commercial;

public class OrganizationSubscriptionRepository : IOrganizationSubscriptionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrganizationSubscriptionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OrganizationSubscriptionOverview?> GetOrganizationSubscriptionOverviewAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT
                o.id AS organization_id,
                o.tenant_id,
                o.name AS organization_name,
                o.location,
                COALESCE(
                    (SELECT u.email
                     FROM user_organization_roles uor
                     JOIN users u ON u.id = uor.user_id
                     JOIN roles r ON r.id = uor.role_id
                     WHERE uor.organization_id = o.id
                       AND r.name IN ('OrganizationAdmin', 'OrgAdmin', 'TenantAdmin', 'Admin')
                       AND u.is_active = TRUE
                     ORDER BY u.created_at ASC
                     LIMIT 1),
                    ''
                ) AS admin_email,
                COALESCE(o.plan_id, '22222222-2222-2222-2222-222222222222'::UUID) AS plan_id,
                COALESCE(NULLIF(o.plan_code, ''), 'PLAN-BLOOM') AS plan_code,
                COALESCE(NULLIF(o.plan_name, ''), 'Bloom (Growth)') AS plan_name,
                o.tariff_id,
                COALESCE(rpt.name, 'Monthly Standard') AS tariff_name,
                COALESCE(rpt.frequency, 'Monthly') AS billing_frequency,
                COALESCE(rpt.price,
                    CASE
                        WHEN o.plan_id = '11111111-1111-1111-1111-111111111111'::UUID THEN 1499
                        WHEN o.plan_id = '33333333-3333-3333-3333-333333333333'::UUID THEN 6999
                        ELSE 2999
                    END
                ) AS current_rate,
                COALESCE(NULLIF(o.currency, ''), 'INR') AS currency,
                o.pricing_region_id,
                o.pricing_region_name,
                COALESCE(o.max_branches, 2) AS max_branches,
                (SELECT COUNT(*)::INT FROM branches b WHERE b.organization_id = o.id AND b.is_active = TRUE) AS current_branches,
                COALESCE(o.max_classrooms, 8) AS max_classrooms,
                (SELECT COUNT(*)::INT FROM classrooms c JOIN branches b ON b.id = c.branch_id WHERE b.organization_id = o.id AND c.is_active = TRUE) AS current_classrooms,
                COALESCE(o.max_children, 90) AS max_children,
                (SELECT COUNT(*)::INT FROM children ch JOIN classrooms c ON ch.classroom_id = c.id JOIN branches b ON c.branch_id = b.id WHERE b.organization_id = o.id AND ch.is_active = TRUE) AS current_children,
                COALESCE(o.includes_ai, TRUE) AS includes_ai,
                COALESCE(
                    (SELECT h.effective_from FROM organization_plan_history h WHERE h.organization_id = o.id AND h.effective_to IS NULL ORDER BY h.effective_from DESC LIMIT 1),
                    o.created_at
                ) AS subscribed_since
            FROM organizations o
            LEFT JOIN region_plan_tariffs rpt ON rpt.id = o.tariff_id
            WHERE o.id = @OrganizationId
            LIMIT 1;";

        return await conn.QueryFirstOrDefaultAsync<OrganizationSubscriptionOverview>(
            new CommandDefinition(sql, new { OrganizationId = organizationId }, cancellationToken: ct));
    }

    public async Task<List<OrganizationPlanHistory>> GetPlanHistoryAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT
                id, tenant_id, organization_id,
                previous_plan_id, previous_plan_code, previous_plan_name,
                previous_tariff_id, previous_tariff_name, previous_rate, previous_currency,
                new_plan_id, new_plan_code, new_plan_name,
                new_tariff_id, new_tariff_name, new_rate, new_currency,
                max_branches, max_classrooms, max_children, includes_ai,
                change_type, reason, changed_by_user_id, changed_by_email,
                effective_from, effective_to, created_at
            FROM organization_plan_history
            WHERE organization_id = @OrganizationId
            ORDER BY effective_from DESC, created_at DESC;";

        var rows = await conn.QueryAsync<OrganizationPlanHistory>(
            new CommandDefinition(sql, new { OrganizationId = organizationId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<List<OrganizationPlanChangeRequest>> GetPlanChangeRequestsByOrgAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT *
            FROM organization_plan_change_requests
            WHERE organization_id = @OrganizationId
            ORDER BY created_at DESC;";

        var rows = await conn.QueryAsync<OrganizationPlanChangeRequest>(
            new CommandDefinition(sql, new { OrganizationId = organizationId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<List<OrganizationPlanChangeRequest>> GetAllPlanChangeRequestsAsync(string? status = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT *
            FROM organization_plan_change_requests
            WHERE (@Status IS NULL OR @Status = '' OR status = @Status)
            ORDER BY
                CASE status
                    WHEN 'PendingPlatformReview' THEN 1
                    WHEN 'PendingOrgAcceptance' THEN 2
                    ELSE 3
                END,
                created_at DESC;";

        var rows = await conn.QueryAsync<OrganizationPlanChangeRequest>(
            new CommandDefinition(sql, new { Status = status }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<OrganizationPlanChangeRequest?> GetPlanChangeRequestByIdAsync(Guid requestId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"SELECT * FROM organization_plan_change_requests WHERE id = @RequestId LIMIT 1;";
        return await conn.QueryFirstOrDefaultAsync<OrganizationPlanChangeRequest>(
            new CommandDefinition(sql, new { RequestId = requestId }, cancellationToken: ct));
    }

    public async Task<OrganizationPlanChangeRequest> CreatePlanChangeRequestAsync(OrganizationPlanChangeRequest request, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        if (request.Id == Guid.Empty) request.Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        request.RequestedAt = now;
        request.CreatedAt = now;
        request.UpdatedAt = now;

        const string sql = @"
            INSERT INTO organization_plan_change_requests (
                id, tenant_id, organization_id, organization_name,
                current_plan_id, current_plan_name, current_tariff_id, current_rate, current_currency,
                requested_plan_id, requested_plan_name, requested_tariff_id, request_type, org_admin_notes,
                status, requested_by_user_id, requested_by_email, requested_at, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @OrganizationName,
                @CurrentPlanId, @CurrentPlanName, @CurrentTariffId, @CurrentRate, @CurrentCurrency,
                @RequestedPlanId, @RequestedPlanName, @RequestedTariffId, @RequestType, @OrgAdminNotes,
                @Status, @RequestedByUserId, @RequestedByEmail, @RequestedAt, @CreatedAt, @UpdatedAt
            );";

        await conn.ExecuteAsync(new CommandDefinition(sql, request, cancellationToken: ct));
        return request;
    }

    public async Task<bool> UpdatePlanChangeRequestProposalAsync(
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
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            UPDATE organization_plan_change_requests
            SET
                proposed_plan_id = @ProposedPlanId,
                proposed_plan_name = @ProposedPlanName,
                proposed_tariff_id = @ProposedTariffId,
                proposed_tariff_name = @ProposedTariffName,
                proposed_rate = @ProposedRate,
                proposed_currency = @ProposedCurrency,
                proposed_effective_date = @ProposedEffectiveDate,
                platform_admin_notes = @PlatformAdminNotes,
                status = 'PendingOrgAcceptance',
                reviewed_by_user_id = @ReviewedByUserId,
                reviewed_by_email = @ReviewedByEmail,
                reviewed_at = NOW(),
                updated_at = NOW()
            WHERE id = @RequestId
              AND status IN ('PendingPlatformReview', 'PendingOrgAcceptance');";

        var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            RequestId = requestId,
            ProposedPlanId = proposedPlanId,
            ProposedPlanName = proposedPlanName,
            ProposedTariffId = proposedTariffId,
            ProposedTariffName = proposedTariffName,
            ProposedRate = proposedRate,
            ProposedCurrency = proposedCurrency,
            ProposedEffectiveDate = proposedEffectiveDate,
            PlatformAdminNotes = platformAdminNotes,
            ReviewedByUserId = reviewedByUserId,
            ReviewedByEmail = reviewedByEmail
        }, cancellationToken: ct));

        return affected > 0;
    }

    public async Task<bool> RejectPlanChangeRequestByPlatformAsync(
        Guid requestId,
        string? platformAdminNotes,
        Guid? reviewedByUserId,
        string? reviewedByEmail,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            UPDATE organization_plan_change_requests
            SET
                platform_admin_notes = @PlatformAdminNotes,
                status = 'RejectedByPlatform',
                reviewed_by_user_id = @ReviewedByUserId,
                reviewed_by_email = @ReviewedByEmail,
                reviewed_at = NOW(),
                updated_at = NOW()
            WHERE id = @RequestId
              AND status IN ('PendingPlatformReview', 'PendingOrgAcceptance');";

        var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            RequestId = requestId,
            PlatformAdminNotes = platformAdminNotes,
            ReviewedByUserId = reviewedByUserId,
            ReviewedByEmail = reviewedByEmail
        }, cancellationToken: ct));

        return affected > 0;
    }

    public async Task<bool> UpdateOrgResponseAsync(
        Guid requestId,
        string status,
        string? orgResponseNotes,
        Guid? respondedByUserId,
        string? respondedByEmail,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            UPDATE organization_plan_change_requests
            SET
                status = @Status,
                org_response_notes = @OrgResponseNotes,
                responded_by_user_id = @RespondedByUserId,
                responded_by_email = @RespondedByEmail,
                responded_at = NOW(),
                updated_at = NOW()
            WHERE id = @RequestId
              AND status = 'PendingOrgAcceptance';";

        var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            RequestId = requestId,
            Status = status,
            OrgResponseNotes = orgResponseNotes,
            RespondedByUserId = respondedByUserId,
            RespondedByEmail = respondedByEmail
        }, cancellationToken: ct));

        return affected > 0;
    }

    public async Task ApplyApprovedPlanChangeAsync(
        Guid organizationId,
        Guid regionId,
        Guid planId,
        Guid? tariffId,
        string currency,
        string changeType,
        string reason,
        Guid? changedByUserId,
        string? changedByEmail,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            CALL sp_assign_organization_regional_plan(
                @OrgId,
                @RegionId,
                @PlanId,
                @TariffId,
                @Currency,
                @ChangeType,
                @Reason,
                @ChangedByUserId,
                @ChangedByEmail
            );";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            OrgId = organizationId,
            RegionId = regionId,
            PlanId = planId,
            TariffId = tariffId,
            Currency = currency,
            ChangeType = changeType,
            Reason = reason,
            ChangedByUserId = changedByUserId,
            ChangedByEmail = changedByEmail ?? "platform-admin@enjoyeveryday.com"
        }, cancellationToken: ct));
    }
}

