using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
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

    public async Task<ActivityRecommendationRequest> CreateRequestAsync(ActivityRecommendationRequest request, IEnumerable<ActivityRecommendationItem> items, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = conn.BeginTransaction();

        const string reqSql = @"
            INSERT INTO activity_recommendation_requests (
                id, tenant_id, organization_id, classroom_id, child_id, requested_by_user_id,
                request_timestamp, scope, context_preferences, evidence_count, created_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @ClassroomId, @ChildId, @RequestedByUserId,
                @RequestTimestamp, @Scope, @ContextPreferences::jsonb, @EvidenceCount, @CreatedAt
            );";

        await conn.ExecuteAsync(reqSql, new
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
        }, transaction);

        const string itemSql = @"
            INSERT INTO activity_recommendation_items (
                id, request_id, tenant_id, experience_id, experience_title,
                suggested_format, estimated_duration_minutes, rationale,
                supporting_evidence, materials, rank_order, is_evidence_limited, created_at
            ) VALUES (
                @Id, @RequestId, @TenantId, @ExperienceId, @ExperienceTitle,
                @SuggestedFormat, @EstimatedDurationMinutes, @Rationale,
                @SupportingEvidence::jsonb, @Materials::jsonb, @RankOrder, @IsEvidenceLimited, @CreatedAt
            );";

        foreach (var item in items)
        {
            await conn.ExecuteAsync(itemSql, new
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
            }, transaction);
        }

        transaction.Commit();
        return request;
    }

    public async Task<ActivityRecommendationRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, tenant_id as TenantId, organization_id as OrganizationId, classroom_id as ClassroomId,
                   child_id as ChildId, requested_by_user_id as RequestedByUserId, request_timestamp as RequestTimestamp,
                   scope, context_preferences as ContextPreferences, evidence_count as EvidenceCount, created_at as CreatedAt
            FROM activity_recommendation_requests
            WHERE id = @RequestId AND tenant_id = @TenantId;";

        return await conn.QuerySingleOrDefaultAsync<ActivityRecommendationRequest>(sql, new
        {
            RequestId = requestId,
            TenantId = _tenantContext.TenantId
        });
    }

    public async Task<IEnumerable<ActivityRecommendationItem>> GetItemsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, request_id as RequestId, tenant_id as TenantId, experience_id as ExperienceId,
                   experience_title as ExperienceTitle, suggested_format as SuggestedFormat,
                   estimated_duration_minutes as EstimatedDurationMinutes, rationale,
                   supporting_evidence as SupportingEvidence, materials as Materials,
                   rank_order as RankOrder, is_evidence_limited as IsEvidenceLimited, created_at as CreatedAt
            FROM activity_recommendation_items
            WHERE request_id = @RequestId AND tenant_id = @TenantId
            ORDER BY rank_order ASC;";

        return await conn.QueryAsync<ActivityRecommendationItem>(sql, new
        {
            RequestId = requestId,
            TenantId = _tenantContext.TenantId
        });
    }

    public async Task<ActivityRecommendationDecision> RecordDecisionAsync(ActivityRecommendationDecision decision, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO activity_recommendation_decisions (
                id, request_id, recommendation_item_id, tenant_id, decided_by_user_id,
                decision, rejection_reason, resulting_schedule_id, decided_at
            ) VALUES (
                @Id, @RequestId, @RecommendationItemId, @TenantId, @DecidedByUserId,
                @Decision, @RejectionReason, @ResultingScheduleId, @DecidedAt
            )
            RETURNING id;";

        await conn.ExecuteAsync(sql, new
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
        });

        return decision;
    }

    public async Task<IEnumerable<ActivityRecommendationDecision>> GetDecisionsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, request_id as RequestId, recommendation_item_id as RecommendationItemId,
                   tenant_id as TenantId, decided_by_user_id as DecidedByUserId, decision,
                   rejection_reason as RejectionReason, resulting_schedule_id as ResultingScheduleId,
                   decided_at as DecidedAt
            FROM activity_recommendation_decisions
            WHERE request_id = @RequestId AND tenant_id = @TenantId
            ORDER BY decided_at DESC;";

        return await conn.QueryAsync<ActivityRecommendationDecision>(sql, new
        {
            RequestId = requestId,
            TenantId = _tenantContext.TenantId
        });
    }
}
