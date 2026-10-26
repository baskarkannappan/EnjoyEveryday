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

public class WeeklyPlanningRepository : IWeeklyPlanningRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WeeklyPlanningRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<WeeklyPlanningDraftRequest> CreateDraftRequestAsync(WeeklyPlanningDraftRequest request, IEnumerable<WeeklyPlanningDraftItem> items, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        if (request.Id == Guid.Empty) request.Id = Guid.NewGuid();

        await conn.ExecuteProcedureAsync(
            "sp_create_weekly_planning_draft_request",
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
                    "sp_create_weekly_planning_draft_item",
                    item,
                    transaction: tx,
                    cancellationToken: cancellationToken);
            }
        }

        tx.Commit();
        request.Items = itemList;
        return request;
    }

    public async Task<WeeklyPlanningDraftRequest?> GetDraftByIdAsync(Guid tenantId, Guid draftId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var draft = await conn.QuerySingleFunctionAsync<WeeklyPlanningDraftRequest>(
            "fn_get_weekly_planning_draft_by_id",
            new { TenantId = tenantId, DraftId = draftId },
            cancellationToken: cancellationToken);

        if (draft != null)
        {
            var items = await GetItemsByRequestIdAsync(tenantId, draft.Id, cancellationToken);
            draft.Items = items.AsList();
        }
        return draft;
    }

    public async Task<WeeklyPlanningDraftRequest?> GetLatestDraftForWeekAsync(Guid tenantId, Guid classroomId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var draft = await conn.QuerySingleFunctionAsync<WeeklyPlanningDraftRequest>(
            "fn_get_latest_weekly_planning_draft_for_week",
            new { TenantId = tenantId, ClassroomId = classroomId, WeekStartDate = weekStartDate },
            cancellationToken: cancellationToken);

        if (draft != null)
        {
            var items = await GetItemsByRequestIdAsync(tenantId, draft.Id, cancellationToken);
            draft.Items = items.AsList();
        }
        return draft;
    }

    public async Task<IEnumerable<WeeklyPlanningDraftItem>> GetItemsByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<WeeklyPlanningDraftItem>(
            "fn_get_weekly_planning_draft_items",
            new { TenantId = tenantId, RequestId = requestId },
            cancellationToken: cancellationToken);
    }

    public async Task UpdateDraftItemAsync(WeeklyPlanningDraftItem item, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_update_weekly_planning_draft_item",
            item,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateDraftStatusAsync(Guid tenantId, Guid draftId, string status, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_update_weekly_planning_draft_status",
            new { TenantId = tenantId, DraftId = draftId, Status = status },
            cancellationToken: cancellationToken);
    }
}
