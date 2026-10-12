using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildDailyCareRepository : IChildDailyCareRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildDailyCareRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // ==========================================
    // 1. CARE EVENTS
    // ==========================================

    public async Task<Guid> AddCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default)
    {
        if (ev.Id == Guid.Empty)
            ev.Id = Guid.NewGuid();

        const string sql = @"
            INSERT INTO child_care_events (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                event_type, event_time, diaper_condition, skin_observation,
                meal_type, food_offered, amount_consumed, fluid_oz,
                rest_status, toileting_activity, hygiene_type, comfort_mood,
                notes, parent_visible, recorded_by_user_id, recorded_by_name,
                correction_reason, is_corrected, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ClassroomId, @ChildId,
                @EventType, @EventTime, @DiaperCondition, @SkinObservation,
                @MealType, @FoodOffered, @AmountConsumed, @FluidOz,
                @RestStatus, @ToiletingActivity, @HygieneType, @ComfortMood,
                @Notes, @ParentVisible, @RecordedByUserId, @RecordedByName,
                @CorrectionReason, @IsCorrected, @CreatedAt, @UpdatedAt
            );";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(sql, ev);
        return ev.Id;
    }

    public async Task UpdateCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default)
    {
        ev.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE child_care_events
            SET event_time = @EventTime,
                diaper_condition = @DiaperCondition,
                skin_observation = @SkinObservation,
                meal_type = @MealType,
                food_offered = @FoodOffered,
                amount_consumed = @AmountConsumed,
                fluid_oz = @FluidOz,
                rest_status = @RestStatus,
                toileting_activity = @ToiletingActivity,
                hygiene_type = @HygieneType,
                comfort_mood = @ComfortMood,
                notes = @Notes,
                parent_visible = @ParentVisible,
                correction_reason = @CorrectionReason,
                is_corrected = TRUE,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(sql, ev);
    }

    public async Task<ChildCareEvent?> GetCareEventByIdAsync(Guid tenantId, Guid eventId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT e.*, 
                   CONCAT(c.first_name, ' ', c.last_name) AS ChildName,
                   cl.name AS ClassroomName
            FROM child_care_events e
            JOIN children c ON e.child_id = c.id
            LEFT JOIN classrooms cl ON e.classroom_id = cl.id
            WHERE e.tenant_id = @TenantId AND e.id = @EventId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<ChildCareEvent>(sql, new { TenantId = tenantId, EventId = eventId });
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT e.*, 
                   CONCAT(c.first_name, ' ', c.last_name) AS ChildName,
                   cl.name AS ClassroomName
            FROM child_care_events e
            JOIN children c ON e.child_id = c.id
            LEFT JOIN classrooms cl ON e.classroom_id = cl.id
            WHERE e.tenant_id = @TenantId 
              AND e.child_id = @ChildId
              AND (e.event_time::date = @Date OR (e.event_time AT TIME ZONE 'America/New_York')::date = @Date)
            ORDER BY e.event_time ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildCareEvent>(sql, new { TenantId = tenantId, ChildId = childId, Date = date.ToDateTime(TimeOnly.MinValue) });
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT e.*, 
                   CONCAT(c.first_name, ' ', c.last_name) AS ChildName,
                   cl.name AS ClassroomName
            FROM child_care_events e
            JOIN children c ON e.child_id = c.id
            LEFT JOIN classrooms cl ON e.classroom_id = cl.id
            WHERE e.tenant_id = @TenantId 
              AND e.child_id = @ChildId
            ORDER BY e.event_time DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildCareEvent>(sql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByClassroomAndDateAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT e.*, 
                   CONCAT(c.first_name, ' ', c.last_name) AS ChildName,
                   cl.name AS ClassroomName
            FROM child_care_events e
            JOIN children c ON e.child_id = c.id
            LEFT JOIN classrooms cl ON e.classroom_id = cl.id
            WHERE e.tenant_id = @TenantId 
              AND e.classroom_id = @ClassroomId
              AND (e.event_time::date = @Date OR (e.event_time AT TIME ZONE 'America/New_York')::date = @Date)
            ORDER BY e.event_time DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildCareEvent>(sql, new { TenantId = tenantId, ClassroomId = classroomId, Date = date.ToDateTime(TimeOnly.MinValue) });
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByBranchAndDateAsync(Guid tenantId, Guid branchId, DateOnly date, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT e.*, 
                   CONCAT(c.first_name, ' ', c.last_name) AS ChildName,
                   cl.name AS ClassroomName
            FROM child_care_events e
            JOIN children c ON e.child_id = c.id
            LEFT JOIN classrooms cl ON e.classroom_id = cl.id
            WHERE e.tenant_id = @TenantId 
              AND e.branch_id = @BranchId
              AND (e.event_time::date = @Date OR (e.event_time AT TIME ZONE 'America/New_York')::date = @Date)
            ORDER BY e.event_time DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildCareEvent>(sql, new { TenantId = tenantId, BranchId = branchId, Date = date.ToDateTime(TimeOnly.MinValue) });
    }

    // ==========================================
    // 2. NAP SESSIONS
    // ==========================================

    public async Task<Guid> StartNapSessionAsync(ChildNapSession session, CancellationToken cancellationToken = default)
    {
        if (session.Id == Guid.Empty)
            session.Id = Guid.NewGuid();

        const string sql = @"
            INSERT INTO child_nap_sessions (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                start_time, status, notes, started_by_user_id, started_by_name,
                parent_visible, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ClassroomId, @ChildId,
                @StartTime, 'Active', @Notes, @StartedByUserId, @StartedByName,
                @ParentVisible, @CreatedAt, @UpdatedAt
            );";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(sql, session);
        return session.Id;
    }

    public async Task EndNapSessionAsync(
        Guid tenantId,
        Guid sessionId,
        DateTime endTime,
        int durationMinutes,
        string? outcome,
        string? notes,
        Guid endedByUserId,
        string? endedByName,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE child_nap_sessions
            SET end_time = @EndTime,
                duration_minutes = @DurationMinutes,
                status = 'Completed',
                outcome = @Outcome,
                notes = COALESCE(@Notes, notes),
                ended_by_user_id = @EndedByUserId,
                ended_by_name = @EndedByName,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @SessionId AND tenant_id = @TenantId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            SessionId = sessionId,
            EndTime = endTime,
            DurationMinutes = durationMinutes,
            Outcome = outcome,
            Notes = notes,
            EndedByUserId = endedByUserId,
            EndedByName = endedByName
        });
    }

    public async Task<ChildNapSession?> GetActiveNapSessionAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.*, CONCAT(c.first_name, ' ', c.last_name) AS ChildName
            FROM child_nap_sessions s
            JOIN children c ON s.child_id = c.id
            WHERE s.tenant_id = @TenantId 
              AND s.child_id = @ChildId 
              AND s.status = 'Active'
            ORDER BY s.start_time DESC
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<ChildNapSession>(sql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task<IEnumerable<ChildNapSession>> GetActiveNapsByClassroomAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.*, CONCAT(c.first_name, ' ', c.last_name) AS ChildName
            FROM child_nap_sessions s
            JOIN children c ON s.child_id = c.id
            WHERE s.tenant_id = @TenantId 
              AND s.classroom_id = @ClassroomId 
              AND s.status = 'Active'
            ORDER BY s.start_time ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildNapSession>(sql, new { TenantId = tenantId, ClassroomId = classroomId });
    }

    public async Task<IEnumerable<ChildNapSession>> GetNapsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.*, CONCAT(c.first_name, ' ', c.last_name) AS ChildName
            FROM child_nap_sessions s
            JOIN children c ON s.child_id = c.id
            WHERE s.tenant_id = @TenantId 
              AND s.child_id = @ChildId
              AND s.start_time::date = @Date
            ORDER BY s.start_time ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildNapSession>(sql, new { TenantId = tenantId, ChildId = childId, Date = date.ToDateTime(TimeOnly.MinValue) });
    }

    // ==========================================
    // 3. CARE PLANS
    // ==========================================

    public async Task<ChildCarePlan?> GetActiveCarePlanAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT p.*, CONCAT(c.first_name, ' ', c.last_name) AS ChildName
            FROM child_care_plans p
            JOIN children c ON p.child_id = c.id
            WHERE p.tenant_id = @TenantId 
              AND p.child_id = @ChildId 
              AND p.status = 'Active'
            ORDER BY p.version DESC
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<ChildCarePlan>(sql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task<Guid> SaveCarePlanAsync(ChildCarePlan plan, CancellationToken cancellationToken = default)
    {
        if (plan.Id == Guid.Empty)
            plan.Id = Guid.NewGuid();

        const string sql = @"
            INSERT INTO child_care_plans (
                id, tenant_id, organization_id, branch_id, child_id, version,
                diaper_routine_guidance, feeding_instructions, bottle_schedule_guidance,
                sleep_routine_guidance, toileting_support_plan, comfort_calming_preferences,
                required_supplies_note, dietary_allergies_summary, effective_date, review_date,
                status, created_by_user_id, approved_by_user_id, approved_at, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ChildId, @Version,
                @DiaperRoutineGuidance, @FeedingInstructions, @BottleScheduleGuidance,
                @SleepRoutineGuidance, @ToiletingSupportPlan, @ComfortCalmingPreferences,
                @RequiredSuppliesNote, @DietaryAllergiesSummary, @EffectiveDate, @ReviewDate,
                @Status, @CreatedByUserId, @ApprovedByUserId, @ApprovedAt, @CreatedAt, @UpdatedAt
            );";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(sql, new
        {
            plan.Id,
            plan.TenantId,
            plan.OrganizationId,
            plan.BranchId,
            plan.ChildId,
            plan.Version,
            plan.DiaperRoutineGuidance,
            plan.FeedingInstructions,
            plan.BottleScheduleGuidance,
            plan.SleepRoutineGuidance,
            plan.ToiletingSupportPlan,
            plan.ComfortCalmingPreferences,
            plan.RequiredSuppliesNote,
            plan.DietaryAllergiesSummary,
            EffectiveDate = plan.EffectiveDate.ToDateTime(TimeOnly.MinValue),
            ReviewDate = plan.ReviewDate.HasValue ? plan.ReviewDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            plan.Status,
            plan.CreatedByUserId,
            plan.ApprovedByUserId,
            plan.ApprovedAt,
            plan.CreatedAt,
            plan.UpdatedAt
        });
        return plan.Id;
    }

    public async Task<IEnumerable<ChildCarePlan>> GetCarePlansByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT p.*, CONCAT(c.first_name, ' ', c.last_name) AS ChildName
            FROM child_care_plans p
            JOIN children c ON p.child_id = c.id
            WHERE p.tenant_id = @TenantId 
              AND p.branch_id = @BranchId 
              AND p.status = 'Active'
            ORDER BY c.first_name ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildCarePlan>(sql, new { TenantId = tenantId, BranchId = branchId });
    }

    // ==========================================
    // 4. CARE SUPPLIES
    // ==========================================

    public async Task<IEnumerable<ChildCareSupply>> GetSuppliesByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.*, 
                   CONCAT(c.first_name, ' ', c.last_name) AS ChildName,
                   cl.name AS ClassroomName
            FROM child_care_supplies s
            JOIN children c ON s.child_id = c.id
            LEFT JOIN classrooms cl ON s.classroom_id = cl.id
            WHERE s.tenant_id = @TenantId AND s.child_id = @ChildId
            ORDER BY s.supply_type ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildCareSupply>(sql, new { TenantId = tenantId, ChildId = childId });
    }

    public async Task<IEnumerable<ChildCareSupply>> GetLowSuppliesByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.*, 
                   CONCAT(c.first_name, ' ', c.last_name) AS ChildName,
                   cl.name AS ClassroomName
            FROM child_care_supplies s
            JOIN children c ON s.child_id = c.id
            LEFT JOIN classrooms cl ON s.classroom_id = cl.id
            WHERE s.tenant_id = @TenantId 
              AND s.branch_id = @BranchId 
              AND s.status IN ('Low', 'Depleted')
            ORDER BY s.updated_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryAsync<ChildCareSupply>(sql, new { TenantId = tenantId, BranchId = branchId });
    }

    public async Task<Guid> SaveSupplyStatusAsync(ChildCareSupply supply, CancellationToken cancellationToken = default)
    {
        if (supply.Id == Guid.Empty)
            supply.Id = Guid.NewGuid();

        supply.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO child_care_supplies (
                id, tenant_id, organization_id, branch_id, classroom_id, child_id,
                supply_type, status, notes, threshold_note, last_reported_by_user_id,
                last_reported_by_name, parent_reminder_sent, last_reminder_at, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ClassroomId, @ChildId,
                @SupplyType, @Status, @Notes, @ThresholdNote, @LastReportedByUserId,
                @LastReportedByName, @ParentReminderSent, @LastReminderAt, @CreatedAt, @UpdatedAt
            )
            ON CONFLICT (id) DO UPDATE
            SET status = EXCLUDED.status,
                notes = EXCLUDED.notes,
                threshold_note = EXCLUDED.threshold_note,
                last_reported_by_user_id = EXCLUDED.last_reported_by_user_id,
                last_reported_by_name = EXCLUDED.last_reported_by_name,
                parent_reminder_sent = EXCLUDED.parent_reminder_sent,
                last_reminder_at = EXCLUDED.last_reminder_at,
                updated_at = EXCLUDED.updated_at;";

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(sql, supply);
        return supply.Id;
    }
}
