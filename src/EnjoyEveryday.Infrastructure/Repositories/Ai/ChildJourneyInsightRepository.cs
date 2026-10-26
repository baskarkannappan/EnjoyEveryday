using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
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

        if (request.Id == Guid.Empty) request.Id = Guid.NewGuid();

        await conn.ExecuteProcedureAsync(
            "sp_create_child_journey_insight_request",
            request,
            transaction: tx,
            cancellationToken: cancellationToken);

        var itemList = items.AsList();
        if (itemList.Count > 0)
        {
            foreach (var item in itemList)
            {
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                item.RequestId = request.Id;
                item.TenantId = request.TenantId;

                await conn.ExecuteProcedureAsync(
                    "sp_create_child_journey_insight_item",
                    item,
                    parameterNames: new[]
                    {
                        "Id", "RequestId", "TenantId", "Category", "Classification",
                        "Statement", "EvidenceStrength", "SupportingEvidence::jsonb",
                        "SuggestedExperienceId", "SuggestedExperienceTitle", "RankOrder", "CreatedAt"
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken);
            }
        }

        tx.Commit();
        request.Items = itemList;
        return request;
    }

    public async Task<ChildJourneyInsightRequest?> GetLatestInsightForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var request = await conn.QuerySingleProcedureAsync<ChildJourneyInsightRequest>(
            "sp_get_latest_child_journey_insight",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);

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
        return await conn.QuerySingleProcedureAsync<ChildJourneyInsightRequest>(
            "sp_get_child_journey_insight_request_by_id",
            new { TenantId = tenantId, RequestId = requestId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildJourneyInsightItem>> GetItemsByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryProcedureAsync<ChildJourneyInsightItem>(
            "sp_get_child_journey_insight_items",
            new { TenantId = tenantId, RequestId = requestId },
            cancellationToken: cancellationToken);
    }
}
