using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildDailyCareRepository : IChildDailyCareRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildDailyCareRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] AddCareEventParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "ClassroomId", "ChildId",
        "EventType", "EventTime", "DiaperCondition", "SkinObservation",
        "MealType", "FoodOffered", "AmountConsumed", "FluidOz",
        "RestStatus", "ToiletingActivity", "HygieneType", "ComfortMood",
        "Notes", "ParentVisible", "RecordedByUserId", "RecordedByName",
        "CorrectionReason", "IsCorrected", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateCareEventParams =
    [
        "Id", "TenantId", "EventTime", "DiaperCondition", "SkinObservation",
        "MealType", "FoodOffered", "AmountConsumed", "FluidOz",
        "RestStatus", "ToiletingActivity", "HygieneType", "ComfortMood",
        "Notes", "ParentVisible", "CorrectionReason", "UpdatedAt"
    ];

    private static readonly string[] StartNapSessionParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "ClassroomId", "ChildId",
        "StartTime", "Notes", "StartedByUserId", "StartedByName",
        "ParentVisible", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] EndNapSessionParams =
    [
        "TenantId", "SessionId", "EndTime", "DurationMinutes", "Outcome", "Notes", "EndedByUserId", "EndedByName"
    ];

    private static readonly string[] SaveCarePlanParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "ChildId", "Version",
        "DiaperRoutineGuidance", "FeedingInstructions", "BottleScheduleGuidance",
        "SleepRoutineGuidance", "ToiletingSupportPlan", "ComfortCalmingPreferences",
        "RequiredSuppliesNote", "DietaryAllergiesSummary", "EffectiveDate", "ReviewDate",
        "Status", "CreatedByUserId", "ApprovedByUserId", "ApprovedAt", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] SaveSupplyStatusParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "ClassroomId", "ChildId",
        "SupplyType", "Status", "Notes", "ThresholdNote", "LastReportedByUserId",
        "LastReportedByName", "ParentReminderSent", "LastReminderAt", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] CreateHandoverParams =
    [
        "Id", "TenantId", "OrganizationId", "BranchId", "ClassroomId", "HandoverDate",
        "ShiftType", "OutgoingTeacherUserId", "OutgoingTeacherName",
        "IncomingTeacherUserId", "IncomingTeacherName", "SummaryNotes",
        "ChildSpecificNotes", "OutstandingCareTasks", "PendingFollowups",
        "Status", "IsCritical", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] AcknowledgeHandoverParams =
    [
        "TenantId", "HandoverId", "UserId", "UserName", "Notes"
    ];

    // ==========================================
    // 1. CARE EVENTS
    // ==========================================

    public async Task<Guid> AddCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default)
    {
        if (ev.Id == Guid.Empty)
            ev.Id = Guid.NewGuid();

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_add_child_care_event",
            ev,
            parameterNames: AddCareEventParams,
            cancellationToken: cancellationToken);

        return ev.Id;
    }

    public async Task UpdateCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default)
    {
        ev.UpdatedAt = DateTime.UtcNow;
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_update_child_care_event",
            ev,
            parameterNames: UpdateCareEventParams,
            cancellationToken: cancellationToken);
    }

    public async Task<ChildCareEvent?> GetCareEventByIdAsync(Guid tenantId, Guid eventId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleFunctionAsync<ChildCareEvent>(
            "fn_get_child_care_event_by_id",
            new { TenantId = tenantId, EventId = eventId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildCareEvent>(
            "fn_get_child_care_events_by_child_and_date",
            new { TenantId = tenantId, ChildId = childId, Date = date.ToDateTime(TimeOnly.MinValue) },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildCareEvent>(
            "fn_get_child_care_events_by_child",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByClassroomAndDateAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildCareEvent>(
            "fn_get_child_care_events_by_classroom_and_date",
            new { TenantId = tenantId, ClassroomId = classroomId, Date = date.ToDateTime(TimeOnly.MinValue) },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildCareEvent>> GetEventsByBranchAndDateAsync(Guid tenantId, Guid branchId, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildCareEvent>(
            "fn_get_child_care_events_by_branch_and_date",
            new { TenantId = tenantId, BranchId = branchId, Date = date.ToDateTime(TimeOnly.MinValue) },
            cancellationToken: cancellationToken);
    }

    // ==========================================
    // 2. NAP SESSIONS
    // ==========================================

    public async Task<Guid> StartNapSessionAsync(ChildNapSession session, CancellationToken cancellationToken = default)
    {
        if (session.Id == Guid.Empty)
            session.Id = Guid.NewGuid();

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_start_child_nap_session",
            session,
            parameterNames: StartNapSessionParams,
            cancellationToken: cancellationToken);

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
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_end_child_nap_session",
            new
            {
                TenantId = tenantId,
                SessionId = sessionId,
                EndTime = endTime,
                DurationMinutes = durationMinutes,
                Outcome = outcome,
                Notes = notes,
                EndedByUserId = endedByUserId,
                EndedByName = endedByName
            },
            parameterNames: EndNapSessionParams,
            cancellationToken: cancellationToken);
    }

    public async Task<ChildNapSession?> GetActiveNapSessionAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleFunctionAsync<ChildNapSession>(
            "fn_get_active_child_nap_session",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildNapSession>> GetActiveNapsByClassroomAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildNapSession>(
            "fn_get_active_child_naps_by_classroom",
            new { TenantId = tenantId, ClassroomId = classroomId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildNapSession>> GetNapsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildNapSession>(
            "fn_get_child_naps_by_child_and_date",
            new { TenantId = tenantId, ChildId = childId, Date = date.ToDateTime(TimeOnly.MinValue) },
            cancellationToken: cancellationToken);
    }

    // ==========================================
    // 3. CARE PLANS
    // ==========================================

    public async Task<ChildCarePlan?> GetActiveCarePlanAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleFunctionAsync<ChildCarePlan>(
            "fn_get_active_child_care_plan",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> SaveCarePlanAsync(ChildCarePlan plan, CancellationToken cancellationToken = default)
    {
        if (plan.Id == Guid.Empty)
            plan.Id = Guid.NewGuid();

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_save_child_care_plan",
            new
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
            },
            parameterNames: SaveCarePlanParams,
            cancellationToken: cancellationToken);

        return plan.Id;
    }

    public async Task<IEnumerable<ChildCarePlan>> GetCarePlansByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildCarePlan>(
            "fn_get_child_care_plans_by_branch",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: cancellationToken);
    }

    // ==========================================
    // 4. CARE SUPPLIES
    // ==========================================

    public async Task<IEnumerable<ChildCareSupply>> GetSuppliesByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildCareSupply>(
            "fn_get_child_care_supplies_by_child",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChildCareSupply>> GetLowSuppliesByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<ChildCareSupply>(
            "fn_get_low_child_care_supplies_by_branch",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> SaveSupplyStatusAsync(ChildCareSupply supply, CancellationToken cancellationToken = default)
    {
        if (supply.Id == Guid.Empty)
            supply.Id = Guid.NewGuid();

        supply.UpdatedAt = DateTime.UtcNow;

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_save_child_care_supply_status",
            supply,
            parameterNames: SaveSupplyStatusParams,
            cancellationToken: cancellationToken);

        return supply.Id;
    }

    public async Task<Guid> CreateHandoverAsync(TeacherHandover handover, CancellationToken cancellationToken = default)
    {
        if (handover.Id == Guid.Empty)
            handover.Id = Guid.NewGuid();

        handover.CreatedAt = DateTime.UtcNow;
        handover.UpdatedAt = DateTime.UtcNow;

        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_create_teacher_handover",
            handover,
            parameterNames: CreateHandoverParams,
            cancellationToken: cancellationToken);

        return handover.Id;
    }

    public async Task AcknowledgeHandoverAsync(Guid tenantId, Guid handoverId, Guid acknowledgedByUserId, string acknowledgedByName, string? notes, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await conn.ExecuteProcedureAsync(
            "sp_acknowledge_teacher_handover",
            new
            {
                TenantId = tenantId,
                HandoverId = handoverId,
                UserId = acknowledgedByUserId,
                UserName = acknowledgedByName,
                Notes = notes
            },
            parameterNames: AcknowledgeHandoverParams,
            cancellationToken: cancellationToken);
    }

    public async Task<TeacherHandover?> GetHandoverByIdAsync(Guid tenantId, Guid handoverId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QuerySingleFunctionAsync<TeacherHandover>(
            "fn_get_teacher_handover_by_id",
            new { TenantId = tenantId, HandoverId = handoverId },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<TeacherHandover>> GetHandoversByClassroomAndDateAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<TeacherHandover>(
            "fn_get_teacher_handovers_by_classroom_and_date",
            new { TenantId = tenantId, ClassroomId = classroomId, Date = date },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<TeacherHandover>> GetPendingHandoversByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<TeacherHandover>(
            "fn_get_pending_teacher_handovers_by_branch",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: cancellationToken);
    }
}
