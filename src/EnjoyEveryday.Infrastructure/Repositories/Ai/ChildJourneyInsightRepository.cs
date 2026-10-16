using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildJourneyInsightRepository : IChildJourneyInsightRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildJourneyInsightRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ChildJourneyInsightRequest> CreateRequestAsync(ChildJourneyInsightRequest request, IEnumerable<ChildJourneyInsightItem> items, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        const string insertRequestSql = @"
            INSERT INTO child_journey_insight_requests (
                id, tenant_id, organization_id, classroom_id, child_id, 
                requested_by_user_id, period_type, period_start_date, period_end_date, 
                focus_area, teacher_question, evidence_count, overall_summary, evidence_gaps, created_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @ClassroomId, @ChildId, 
                @RequestedByUserId, @PeriodType, @PeriodStartDate, @PeriodEndDate, 
                @FocusArea, @TeacherQuestion, @EvidenceCount, @OverallSummary, @EvidenceGaps, @CreatedAt
            );";

        await conn.ExecuteAsync(insertRequestSql, request, tx);

        const string insertItemSql = @"
            INSERT INTO child_journey_insight_items (
                id, request_id, tenant_id, category, classification, 
                statement, evidence_strength, supporting_evidence, 
                suggested_experience_id, suggested_experience_title, rank_order, created_at
            ) VALUES (
                @Id, @RequestId, @TenantId, @Category, @Classification, 
                @Statement, @EvidenceStrength, @SupportingEvidence::jsonb, 
                @SuggestedExperienceId, @SuggestedExperienceTitle, @RankOrder, @CreatedAt
            );";

        var itemList = items.AsList();
        if (itemList.Count > 0)
        {
            await conn.ExecuteAsync(insertItemSql, itemList, tx);
        }

        tx.Commit();
        request.Items = itemList;
        return request;
    }

    public async Task<ChildJourneyInsightRequest?> GetLatestInsightForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT 
                id AS Id,
                tenant_id AS TenantId,
                organization_id AS OrganizationId,
                classroom_id AS ClassroomId,
                child_id AS ChildId,
                requested_by_user_id AS RequestedByUserId,
                period_type AS PeriodType,
                period_start_date AS PeriodStartDate,
                period_end_date AS PeriodEndDate,
                focus_area AS FocusArea,
                teacher_question AS TeacherQuestion,
                evidence_count AS EvidenceCount,
                overall_summary AS OverallSummary,
                evidence_gaps AS EvidenceGaps,
                created_at AS CreatedAt
            FROM child_journey_insight_requests
            WHERE tenant_id = @TenantId AND child_id = @ChildId
            ORDER BY created_at DESC
            LIMIT 1;";

        var request = await conn.QuerySingleOrDefaultAsync<ChildJourneyInsightRequest>(sql, new { TenantId = tenantId, ChildId = childId });
        if (request != null)
        {
            var items = await GetItemsByRequestIdAsync(tenantId, request.Id, cancellationToken);
            request.Items = items.AsList();
        }
        return request;
    }

    public async Task<ChildJourneyInsightRequest?> GetRequestByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT 
                id AS Id,
                tenant_id AS TenantId,
                organization_id AS OrganizationId,
                classroom_id AS ClassroomId,
                child_id AS ChildId,
                requested_by_user_id AS RequestedByUserId,
                period_type AS PeriodType,
                period_start_date AS PeriodStartDate,
                period_end_date AS PeriodEndDate,
                focus_area AS FocusArea,
                teacher_question AS TeacherQuestion,
                evidence_count AS EvidenceCount,
                overall_summary AS OverallSummary,
                evidence_gaps AS EvidenceGaps,
                created_at AS CreatedAt
            FROM child_journey_insight_requests
            WHERE tenant_id = @TenantId AND id = @RequestId;";

        return await conn.QuerySingleOrDefaultAsync<ChildJourneyInsightRequest>(sql, new { TenantId = tenantId, RequestId = requestId });
    }

    public async Task<IEnumerable<ChildJourneyInsightItem>> GetItemsByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT 
                id AS Id,
                request_id AS RequestId,
                tenant_id AS TenantId,
                category AS Category,
                classification AS Classification,
                statement AS Statement,
                evidence_strength AS EvidenceStrength,
                supporting_evidence::text AS SupportingEvidence,
                suggested_experience_id AS SuggestedExperienceId,
                suggested_experience_title AS SuggestedExperienceTitle,
                rank_order AS RankOrder,
                created_at AS CreatedAt
            FROM child_journey_insight_items
            WHERE tenant_id = @TenantId AND request_id = @RequestId
            ORDER BY rank_order ASC;";

        return await conn.QueryAsync<ChildJourneyInsightItem>(sql, new { TenantId = tenantId, RequestId = requestId });
    }
}
