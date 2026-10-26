using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ActivityRecommendationRepository : IActivityRecommendationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public ActivityRecommendationRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
    }

    private static readonly string[] CreateRequestParams =
    [
        "Id", "TenantId", "OrganizationId", "ClassroomId", "ChildId", "RequestedByUserId",
        "RequestTimestamp", "Scope", "ContextPreferences::jsonb", "EvidenceCount", "CreatedAt"
    ];

    private static readonly string[] CreateItemParams =
    [
        "Id", "RequestId", "TenantId", "ExperienceId", "ExperienceTitle",
        "SuggestedFormat", "EstimatedDurationMinutes", "Rationale",
        "SupportingEvidence::jsonb", "Materials::jsonb", "RankOrder", "IsEvidenceLimited", "CreatedAt"
    ];

    private static readonly string[] RecordDecisionParams =
    [
        "Id", "RequestId", "RecommendationItemId", "TenantId", "DecidedByUserId",
        "Decision", "RejectionReason", "ResultingScheduleId", "DecidedAt"
    ];

    public async Task<ActivityRecommendationRequest> CreateRequestAsync(ActivityRecommendationRequest request, IEnumerable<ActivityRecommendationItem> items, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = conn.BeginTransaction();

        await conn.ExecuteProcedureAsync(
            "sp_create_activity_recommendation_request",
            new
            {
                request.Id,
                TenantId = _tenantContext.TenantId,
                request.OrganizationId,
                request.ClassroomId,
                request.ChildId,
                request.RequestedByUserId,
                request.RequestTimestamp,
                request.Scope,
                request.ContextPreferences,
                request.EvidenceCount,
                request.CreatedAt
            },
            parameterNames: CreateRequestParams,
            transaction: transaction,
            cancellationToken: cancellationToken);

        foreach (var item in items)
        {
            await conn.ExecuteProcedureAsync(
                "sp_create_activity_recommendation_item",
                new
                {
                    item.Id,
                    item.RequestId,
                    TenantId = _tenantContext.TenantId,
                    item.ExperienceId,
                    item.ExperienceTitle,
                    item.SuggestedFormat,
                    item.EstimatedDurationMinutes,
                    item.Rationale,
                    item.SupportingEvidence,
                    item.Materials,
                    item.RankOrder,
                    item.IsEvidenceLimited,
                    item.CreatedAt
                },
                parameterNames: CreateItemParams,
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        transaction.Commit();
        return request;
    }

    public async Task<ActivityRecommendationRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleFunctionAsync<ActivityRecommendationRequest>(
            "fn_get_activity_recommendation_request_by_id",
            new
            {
                TenantId = _tenantContext.TenantId,
                RequestId = requestId
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ActivityRecommendationItem>> GetItemsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ActivityRecommendationItem>(
            "fn_get_activity_recommendation_items",
            new
            {
                TenantId = _tenantContext.TenantId,
                RequestId = requestId
            },
            cancellationToken: cancellationToken);
    }

    public async Task<ActivityRecommendationDecision> RecordDecisionAsync(ActivityRecommendationDecision decision, CancellationToken cancellationToken = default)
    {
        if (decision.Id == Guid.Empty) decision.Id = Guid.NewGuid();

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_record_activity_recommendation_decision",
            new
            {
                decision.Id,
                decision.RequestId,
                decision.RecommendationItemId,
                TenantId = _tenantContext.TenantId,
                decision.DecidedByUserId,
                decision.Decision,
                decision.RejectionReason,
                decision.ResultingScheduleId,
                decision.DecidedAt
            },
            parameterNames: RecordDecisionParams,
            cancellationToken: cancellationToken);

        return decision;
    }

    public async Task<IEnumerable<ActivityRecommendationDecision>> GetDecisionsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ActivityRecommendationDecision>(
            "fn_get_activity_recommendation_decisions",
            new
            {
                TenantId = _tenantContext.TenantId,
                RequestId = requestId
            },
            cancellationToken: cancellationToken);
    }
}
