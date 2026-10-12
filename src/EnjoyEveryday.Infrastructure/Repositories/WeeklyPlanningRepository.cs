using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
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

        const string insertRequestSql = @"
            INSERT INTO weekly_planning_draft_requests (
                id, tenant_id, organization_id, classroom_id, requested_by_user_id, 
                week_start_date, week_end_date, focus_areas, indoor_outdoor_preference, 
                materials_notes, status, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @ClassroomId, @RequestedByUserId, 
                @WeekStartDate, @WeekEndDate, @FocusAreas, @IndoorOutdoorPreference, 
                @MaterialsNotes, @Status, @CreatedAt, @UpdatedAt
            );";

        await conn.ExecuteAsync(insertRequestSql, request, tx);

        const string insertItemSql = @"
            INSERT INTO weekly_planning_draft_items (
                id, request_id, tenant_id, scheduled_date, day_of_week, time_of_day, 
                experience_id, experience_title, learning_goal, group_format, 
                estimated_duration_minutes, required_materials, rationale, 
                validation_status, validation_notes, resulting_schedule_id, created_at
            ) VALUES (
                @Id, @RequestId, @TenantId, @ScheduledDate, @DayOfWeek, @TimeOfDay, 
                @ExperienceId, @ExperienceTitle, @LearningGoal, @GroupFormat, 
                @EstimatedDurationMinutes, @RequiredMaterials, @Rationale, 
                @ValidationStatus, @ValidationNotes, @ResultingScheduleId, @CreatedAt
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

    public async Task<WeeklyPlanningDraftRequest?> GetDraftByIdAsync(Guid tenantId, Guid draftId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT 
                id AS Id,
                tenant_id AS TenantId,
                organization_id AS OrganizationId,
                classroom_id AS ClassroomId,
                requested_by_user_id AS RequestedByUserId,
                week_start_date AS WeekStartDate,
                week_end_date AS WeekEndDate,
                focus_areas AS FocusAreas,
                indoor_outdoor_preference AS IndoorOutdoorPreference,
                materials_notes AS MaterialsNotes,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM weekly_planning_draft_requests
            WHERE tenant_id = @TenantId AND id = @DraftId;";

        var draft = await conn.QuerySingleOrDefaultAsync<WeeklyPlanningDraftRequest>(sql, new { TenantId = tenantId, DraftId = draftId });
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
        const string sql = @"
            SELECT 
                id AS Id,
                tenant_id AS TenantId,
                organization_id AS OrganizationId,
                classroom_id AS ClassroomId,
                requested_by_user_id AS RequestedByUserId,
                week_start_date AS WeekStartDate,
                week_end_date AS WeekEndDate,
                focus_areas AS FocusAreas,
                indoor_outdoor_preference AS IndoorOutdoorPreference,
                materials_notes AS MaterialsNotes,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM weekly_planning_draft_requests
            WHERE tenant_id = @TenantId AND classroom_id = @ClassroomId AND week_start_date = @WeekStartDate
            ORDER BY created_at DESC
            LIMIT 1;";

        var draft = await conn.QuerySingleOrDefaultAsync<WeeklyPlanningDraftRequest>(sql, new { TenantId = tenantId, ClassroomId = classroomId, WeekStartDate = weekStartDate });
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
        const string sql = @"
            SELECT 
                id AS Id,
                request_id AS RequestId,
                tenant_id AS TenantId,
                scheduled_date AS ScheduledDate,
                day_of_week AS DayOfWeek,
                time_of_day AS TimeOfDay,
                experience_id AS ExperienceId,
                experience_title AS ExperienceTitle,
                learning_goal AS LearningGoal,
                group_format AS GroupFormat,
                estimated_duration_minutes AS EstimatedDurationMinutes,
                required_materials AS RequiredMaterials,
                rationale AS Rationale,
                validation_status AS ValidationStatus,
                validation_notes AS ValidationNotes,
                resulting_schedule_id AS ResultingScheduleId,
                created_at AS CreatedAt
            FROM weekly_planning_draft_items
            WHERE tenant_id = @TenantId AND request_id = @RequestId
            ORDER BY scheduled_date ASC, time_of_day ASC;";

        return await conn.QueryAsync<WeeklyPlanningDraftItem>(sql, new { TenantId = tenantId, RequestId = requestId });
    }

    public async Task UpdateDraftItemAsync(WeeklyPlanningDraftItem item, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE weekly_planning_draft_items SET
                scheduled_date = @ScheduledDate,
                day_of_week = @DayOfWeek,
                time_of_day = @TimeOfDay,
                experience_id = @ExperienceId,
                experience_title = @ExperienceTitle,
                learning_goal = @LearningGoal,
                group_format = @GroupFormat,
                estimated_duration_minutes = @EstimatedDurationMinutes,
                required_materials = @RequiredMaterials,
                rationale = @Rationale,
                validation_status = @ValidationStatus,
                validation_notes = @ValidationNotes,
                resulting_schedule_id = @ResultingScheduleId
            WHERE id = @Id AND tenant_id = @TenantId;";

        await conn.ExecuteAsync(sql, item);
    }

    public async Task UpdateDraftStatusAsync(Guid tenantId, Guid draftId, string status, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE weekly_planning_draft_requests SET
                status = @Status,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @DraftId AND tenant_id = @TenantId;";

        await conn.ExecuteAsync(sql, new { TenantId = tenantId, DraftId = draftId, Status = status });
    }
}
