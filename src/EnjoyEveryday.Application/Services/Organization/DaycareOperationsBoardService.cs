using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class OperationsBoardFilter
{
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public DateTime? Date { get; set; }
    public string? RoleOverride { get; set; } // For admins testing/switching persona views
}

public class OperationsBoardModel
{
    public string EffectiveRole { get; set; } = RoleTemplates.OrganizationAdmin; // PlatformAdmin, OrganizationAdmin, BranchAdmin, ClassroomTeacher
    public DateTime BoardDate { get; set; } = DateTime.Today;
    public string OrganizationName { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string? SelectedBranchName { get; set; }
    public Guid? SelectedBranchId { get; set; }
    public string? SelectedClassroomName { get; set; }
    public Guid? SelectedClassroomId { get; set; }

    // Summary counts
    public int ExpectedChildrenCount { get; set; }
    public int CheckedInChildrenCount { get; set; }
    public int CheckedOutChildrenCount { get; set; }
    public int ExpectedNotArrivedCount { get; set; }
    public int AbsentChildrenCount { get; set; }

    public int ExpectedStaffCount { get; set; }
    public int PresentStaffCount { get; set; }
    public int AbsentStaffCount { get; set; }

    public int TotalClassroomsCount { get; set; }
    public int OvercapacityClassroomsCount { get; set; }
    public int UnderstaffedClassroomsCount { get; set; }

    public int PlannedExperiencesCount { get; set; }
    public int InProgressExperiencesCount { get; set; }
    public int CompletedExperiencesCount { get; set; }

    public int MissingCareRecordsCount { get; set; }
    public int LatePickupsCount { get; set; }
    public int UnresolvedSafetyActionsCount { get; set; }
    public int PendingEnrollmentsCount { get; set; }

    // Role-specific sections
    public List<ClassroomBoardItem> Classrooms { get; set; } = new();
    public List<OperationalAlertItem> Alerts { get; set; } = new();
    public List<RhythmScheduleItem> TodayRhythm { get; set; } = new();
    public List<TeacherChildCareItem> TeacherChildrenCareStatus { get; set; } = new();
    public List<ReadinessChecklistItem> ReadinessChecklist { get; set; } = new();

    // Financial (only populated for authorized finance roles)
    public bool HasFinanceAccess { get; set; }
    public decimal OutstandingReceivables { get; set; }
    public int OverdueInvoicesCount { get; set; }
}

public class ClassroomBoardItem
{
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int EnrolledChildren { get; set; }
    public int PresentChildren { get; set; }
    public int ExpectedChildren { get; set; }
    public int OccupancyPercentage => Capacity > 0 ? (int)Math.Round((double)PresentChildren / Capacity * 100) : 0;
    public bool IsOverCapacity => PresentChildren > Capacity && Capacity > 0;
    public int PresentStaff { get; set; }
    public int RequiredStaff { get; set; }
    public bool IsUnderstaffed => PresentStaff < RequiredStaff && PresentChildren > 0;
    public string? ActiveExperienceTitle { get; set; }
    public string? ActiveExperienceStatus { get; set; }
    public string? StatusPillCss => IsOverCapacity ? "badge-danger" : (IsUnderstaffed ? "badge-warning" : "badge-success");
}

public class OperationalAlertItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AlertType { get; set; } = "Operational"; // Capacity, Staffing, CareRecord, Safety, Pickup, Finance, Enrollment
    public string Severity { get; set; } = "Medium"; // High, Medium, Info
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string ActionText { get; set; } = "Review";
    public string SourceContext { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class RhythmScheduleItem
{
    public Guid Id { get; set; }
    public bool IsCelebration { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TimeDisplay { get; set; } = string.Empty;
    public string ScopeDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Planned, InProgress, Completed
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string[] PhotoUrls { get; set; } = Array.Empty<string>();
    public string? PreparationNotes { get; set; }
}

public class TeacherChildCareItem
{
    public Guid ChildId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsCheckedIn { get; set; }
    public TimeSpan? ArrivalTime { get; set; }
    public int CareEventsCount { get; set; }
    public string LastRecordedCareType { get; set; } = "None";
    public DateTime? LastRecordedTime { get; set; }
    public bool NeedsAttention => IsCheckedIn && CareEventsCount == 0;
    public string? DietaryOrAllergyNote { get; set; }
}

public class ReadinessChecklistItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string Category { get; set; } = "Opening"; // Opening, Safety, Material
}

public interface IDaycareOperationsBoardService
{
    Task<OperationsBoardModel> GetOperationsBoardAsync(OperationsBoardFilter filter, CancellationToken ct = default);
}

public class DaycareOperationsBoardService : IDaycareOperationsBoardService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAccessScopeRepository _accessScopeRepo;
    private readonly TimeProvider _timeProvider;

    public DaycareOperationsBoardService(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAccessScopeRepository accessScopeRepo,
        TimeProvider timeProvider)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _accessScopeRepo = accessScopeRepo;
        _timeProvider = timeProvider;
    }

    public async Task<OperationsBoardModel> GetOperationsBoardAsync(OperationsBoardFilter filter, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _timeProvider.GetLocalNow();
        var date = filter.Date?.Date ?? now.Date;
        var dateOnly = DateOnly.FromDateTime(date);

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // 1. Resolve Organization
        var orgId = filter.OrganizationId ?? _userContext.OrganizationId;
        if (orgId == null || orgId == Guid.Empty)
        {
            orgId = await conn.QueryFirstOrDefaultAsync<Guid>(
                "SELECT id FROM organizations WHERE tenant_id = @TenantId AND is_active = true ORDER BY name LIMIT 1",
                new { TenantId = tenantId });
        }

        var orgName = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = orgId, TenantId = tenantId }) ?? "EnjoyEveryday Daycare";

        // 2. Resolve Role Scope
        var effectiveRole = ResolveEffectiveRole(filter.RoleOverride);

        // Get user branch/classroom assignments
        var assignedBranchIds = (await _accessScopeRepo.GetAssignedBranchIdsAsync(tenantId, orgId.Value, _userContext.UserId, ct)).ToList();
        var assignedClassroomIds = (await _accessScopeRepo.GetAssignedClassroomIdsAsync(tenantId, _userContext.UserId, ct)).ToList();

        // If teacher, default branch/classroom if none provided
        Guid? filterBranchId = filter.BranchId;
        Guid? filterClassroomId = filter.ClassroomId;

        if (effectiveRole == RoleTemplates.ClassroomTeacher || effectiveRole == RoleTemplates.DaycareTeacher)
        {
            if (!filterClassroomId.HasValue && assignedClassroomIds.Any())
            {
                filterClassroomId = assignedClassroomIds.First();
            }
        }

        if (effectiveRole == RoleTemplates.BranchAdmin)
        {
            if (!filterBranchId.HasValue && assignedBranchIds.Any())
            {
                filterBranchId = assignedBranchIds.First();
            }
        }

        var model = new OperationsBoardModel
        {
            EffectiveRole = effectiveRole,
            BoardDate = date,
            OrganizationId = orgId.Value,
            OrganizationName = orgName,
            SelectedBranchId = filterBranchId,
            SelectedClassroomId = filterClassroomId
        };

        // 3. Classrooms & Occupancy Query
        const string classroomsSql = @"
            SELECT 
                cl.id AS ClassroomId,
                cl.name AS ClassroomName,
                cl.age_group AS AgeGroup,
                cl.branch_id AS BranchId,
                b.name AS BranchName,
                cl.capacity AS Capacity,
                cl.current_enrollment AS EnrolledChildren,
                COALESCE((
                    SELECT COUNT(1) FROM child_attendance ca
                    WHERE ca.classroom_id = cl.id AND ca.attendance_date = @Date 
                      AND ca.status IN ('Present', 'Late') AND ca.tenant_id = @TenantId
                ), 0) AS PresentChildren,
                COALESCE((
                    SELECT COUNT(1) FROM children ch
                    WHERE ch.classroom_id = cl.id AND ch.is_active = true AND ch.tenant_id = @TenantId
                ), 0) AS ExpectedChildren,
                COALESCE((
                    SELECT COUNT(1) FROM teacher_attendance ta
                    WHERE ta.branch_id = cl.branch_id AND ta.attendance_date = @Date
                      AND ta.status = 'Present' AND ta.tenant_id = @TenantId
                ), 0) AS PresentStaff
            FROM classrooms cl
            JOIN branches b ON cl.branch_id = b.id
            WHERE cl.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND cl.is_active = true
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
            ORDER BY b.name, cl.name";

        var classroomRows = (await conn.QueryAsync<ClassroomBoardItem>(classroomsSql, new
        {
            TenantId = tenantId,
            OrgId = orgId.Value,
            Date = date,
            BranchId = filterBranchId,
            ClassroomId = filterClassroomId
        })).ToList();

        // Compute staff requirements based on age group
        foreach (var cr in classroomRows)
        {
            cr.RequiredStaff = cr.AgeGroup switch
            {
                var ag when ag.Contains("Infant", StringComparison.OrdinalIgnoreCase) => Math.Max(1, (int)Math.Ceiling(cr.PresentChildren / 4.0)),
                var ag when ag.Contains("Toddler", StringComparison.OrdinalIgnoreCase) => Math.Max(1, (int)Math.Ceiling(cr.PresentChildren / 5.0)),
                var ag when ag.Contains("Preschool", StringComparison.OrdinalIgnoreCase) => Math.Max(1, (int)Math.Ceiling(cr.PresentChildren / 8.0)),
                _ => Math.Max(1, (int)Math.Ceiling(cr.PresentChildren / 10.0))
            };
        }

        model.Classrooms = classroomRows;
        model.TotalClassroomsCount = classroomRows.Count;
        model.OvercapacityClassroomsCount = classroomRows.Count(c => c.IsOverCapacity);
        model.UnderstaffedClassroomsCount = classroomRows.Count(c => c.IsUnderstaffed);

        // 4. Children Summary Query
        const string childAttendanceSql = @"
            SELECT 
                COUNT(1) FILTER (WHERE ca.status IN ('Present', 'Late')) AS CheckedIn,
                COUNT(1) FILTER (WHERE ca.departure_time IS NOT NULL OR ca.status = 'Departed') AS CheckedOut,
                COUNT(1) FILTER (WHERE ca.status = 'Absent') AS Absent
            FROM child_attendance ca
            JOIN classrooms cl ON ca.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE ca.tenant_id = @TenantId 
              AND b.organization_id = @OrgId
              AND ca.attendance_date = @Date
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)";

        var attRow = await conn.QueryFirstOrDefaultAsync<dynamic>(childAttendanceSql, new
        {
            TenantId = tenantId,
            OrgId = orgId.Value,
            Date = date,
            BranchId = filterBranchId,
            ClassroomId = filterClassroomId
        });

        int checkedIn = Convert.ToInt32(attRow?.checkedin ?? 0);
        int checkedOut = Convert.ToInt32(attRow?.checkedout ?? 0);
        int absent = Convert.ToInt32(attRow?.absent ?? 0);

        int totalExpected = classroomRows.Sum(c => c.ExpectedChildren);
        int expectedNotArrived = Math.Max(0, totalExpected - checkedIn - absent);

        model.ExpectedChildrenCount = totalExpected;
        model.CheckedInChildrenCount = checkedIn;
        model.CheckedOutChildrenCount = checkedOut;
        model.AbsentChildrenCount = absent;
        model.ExpectedNotArrivedCount = expectedNotArrived;

        // 5. Staff Summary Query
        const string staffSql = @"
            SELECT 
                COUNT(1) AS ExpectedStaff,
                COUNT(1) FILTER (WHERE ta.status = 'Present') AS PresentStaff,
                COUNT(1) FILTER (WHERE ta.status = 'Absent') AS AbsentStaff
            FROM teachers t
            LEFT JOIN teacher_attendance ta ON ta.teacher_id = t.id AND ta.attendance_date = @Date AND ta.tenant_id = @TenantId
            WHERE t.tenant_id = @TenantId AND t.status != 'Inactive'
              AND EXISTS (
                  SELECT 1 FROM organization_memberships om
                  WHERE om.user_id = t.user_id AND om.organization_id = @OrgId AND om.status = 'Active'
              )";

        var staffRow = await conn.QueryFirstOrDefaultAsync<dynamic>(staffSql, new
        {
            TenantId = tenantId,
            OrgId = orgId.Value,
            Date = date
        });

        model.ExpectedStaffCount = Convert.ToInt32(staffRow?.expectedstaff ?? 0);
        model.PresentStaffCount = Convert.ToInt32(staffRow?.presentstaff ?? 0);
        model.AbsentStaffCount = Convert.ToInt32(staffRow?.absentstaff ?? 0);

        // 6. Experience & Rhythm Schedules Query
        const string expScheduleSql = @"
            SELECT 
                es.id,
                false AS IsCelebration,
                COALESCE(e.title, 'Classroom Activity') AS Title,
                COALESCE(es.time_of_day, 'Morning') AS TimeDisplay,
                cl.name AS ScopeDisplay,
                es.status AS Status,
                es.classroom_id AS ClassroomId,
                cl.name AS ClassroomName,
                es.preparation_notes AS PreparationNotes
            FROM experience_schedules es
            JOIN classrooms cl ON es.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            LEFT JOIN experiences e ON es.experience_id = e.id
            WHERE es.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND es.scheduled_date = @Date
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)
            ORDER BY es.planned_start_time NULLS LAST, es.time_of_day";

        var schedules = (await conn.QueryAsync<RhythmScheduleItem>(expScheduleSql, new
        {
            TenantId = tenantId,
            OrgId = orgId.Value,
            Date = date,
            BranchId = filterBranchId,
            ClassroomId = filterClassroomId
        })).ToList();

        // Add today's celebrations/events
        const string eventsSql = @"
            SELECT 
                ev.id,
                true AS IsCelebration,
                ev.title AS Title,
                COALESCE(TO_CHAR(ev.start_time, 'HH24:MI'), 'All Day') AS TimeDisplay,
                CASE WHEN ev.is_organization_wide THEN 'All Campus' ELSE 'Classroom Event' END AS ScopeDisplay,
                ev.status AS Status,
                NULL::uuid AS BranchId,
                NULL::text[] AS PhotoUrls,
                ev.description AS PreparationNotes
            FROM events ev
            WHERE ev.tenant_id = @TenantId
              AND ev.event_date = @Date
            ORDER BY ev.start_time NULLS LAST";

        var eventRows = (await conn.QueryAsync<RhythmScheduleItem>(eventsSql, new
        {
            TenantId = tenantId,
            Date = date
        })).ToList();

        var combinedRhythm = schedules.Concat(eventRows).ToList();
        model.TodayRhythm = combinedRhythm;
        model.PlannedExperiencesCount = combinedRhythm.Count(r => r.Status == "Planned" || r.Status == "Scheduled" || r.Status == "Confirmed");
        model.InProgressExperiencesCount = combinedRhythm.Count(r => r.Status == "Started" || r.Status == "Active" || r.Status == "InProgress");
        model.CompletedExperiencesCount = combinedRhythm.Count(r => r.Status == "Completed" || r.Status == "Finished");

        // Link active experience title to classrooms
        foreach (var cr in model.Classrooms)
        {
            var active = schedules.FirstOrDefault(s => s.ClassroomId == cr.ClassroomId && (s.Status == "Started" || s.Status == "Active" || s.Status == "InProgress"));
            if (active != null)
            {
                cr.ActiveExperienceTitle = active.Title;
                cr.ActiveExperienceStatus = active.Status;
            }
        }

        // 7. Missing Daily Care Records Query
        const string missingCareSql = @"
            SELECT 
                c.id AS ChildId,
                c.first_name || ' ' || c.last_name AS FullName,
                c.photo_url AS PhotoUrl,
                true AS IsCheckedIn,
                ca.arrival_time AS ArrivalTime,
                COALESCE((
                    SELECT COUNT(1) FROM child_care_events cce 
                    WHERE cce.child_id = c.id AND cce.tenant_id = @TenantId 
                      AND cce.event_time >= @StartOfDay AND cce.event_time <= @EndOfDay
                ), 0) AS CareEventsCount,
                (
                    SELECT cce.event_type FROM child_care_events cce
                    WHERE cce.child_id = c.id AND cce.tenant_id = @TenantId 
                      AND cce.event_time >= @StartOfDay AND cce.event_time <= @EndOfDay
                    ORDER BY cce.event_time DESC LIMIT 1
                ) AS LastRecordedCareType,
                (
                    SELECT cce.event_time FROM child_care_events cce
                    WHERE cce.child_id = c.id AND cce.tenant_id = @TenantId 
                      AND cce.event_time >= @StartOfDay AND cce.event_time <= @EndOfDay
                    ORDER BY cce.event_time DESC LIMIT 1
                ) AS LastRecordedTime
            FROM children c
            JOIN child_attendance ca ON c.id = ca.child_id AND ca.attendance_date = @Date AND ca.status IN ('Present', 'Late')
            JOIN classrooms cl ON c.classroom_id = cl.id
            JOIN branches b ON cl.branch_id = b.id
            WHERE c.tenant_id = @TenantId
              AND b.organization_id = @OrgId
              AND (@BranchId IS NULL OR cl.branch_id = @BranchId)
              AND (@ClassroomId IS NULL OR cl.id = @ClassroomId)";

        var careList = (await conn.QueryAsync<TeacherChildCareItem>(missingCareSql, new
        {
            TenantId = tenantId,
            OrgId = orgId.Value,
            Date = date,
            StartOfDay = date.Date,
            EndOfDay = date.Date.AddDays(1).AddTicks(-1),
            BranchId = filterBranchId,
            ClassroomId = filterClassroomId
        })).ToList();

        model.TeacherChildrenCareStatus = careList;
        model.MissingCareRecordsCount = careList.Count(c => c.NeedsAttention);

        // 8. Late Pickups Query (children present past 17:00 or departure time passed)
        if (now.TimeOfDay > new TimeSpan(16, 30, 0))
        {
            model.LatePickupsCount = careList.Count(c => c.IsCheckedIn);
        }

        // 9. Pending Enrollments Query
        const string pendingEnrollmentsSql = @"
            SELECT COUNT(1) FROM children c
            LEFT JOIN classrooms cl ON c.classroom_id = cl.id
            LEFT JOIN branches b ON cl.branch_id = b.id
            WHERE c.tenant_id = @TenantId
              AND (@OrgId IS NULL OR b.organization_id = @OrgId OR c.classroom_id IS NULL)
              AND (c.enrollment_status = 'Draft' OR c.profile_completion_percentage < 100)";

        model.PendingEnrollmentsCount = await conn.ExecuteScalarAsync<int>(pendingEnrollmentsSql, new
        {
            TenantId = tenantId,
            OrgId = orgId.Value
        });

        // 10. Financial Summary (Strictly permission-gated)
        if (_userContext.HasPermission(Permissions.OrganizationBillingView) || _userContext.HasPermission(Permissions.PlatformBillingManage))
        {
            model.HasFinanceAccess = true;
            const string finSql = @"
                SELECT 
                    COALESCE(SUM(total_amount - amount_paid) FILTER (WHERE status != 'Paid' AND status != 'Void'), 0) AS Outstanding,
                    COUNT(1) FILTER (WHERE status != 'Paid' AND status != 'Void' AND due_date < NOW()) AS OverdueCount
                FROM invoices
                WHERE tenant_id = @TenantId
                  AND organization_id = @OrgId";

            var finRow = await conn.QueryFirstOrDefaultAsync<dynamic>(finSql, new { TenantId = tenantId, OrgId = orgId.Value });
            model.OutstandingReceivables = Convert.ToDecimal(finRow?.outstanding ?? 0);
            model.OverdueInvoicesCount = Convert.ToInt32(finRow?.overduecount ?? 0);
        }

        // 10b. Teacher Handovers Query
        try
        {
            const string handoversSql = @"
                SELECT 
                    th.id, th.classroom_id AS ClassroomId, cl.name AS ClassroomName,
                    th.shift_type AS ShiftType, th.outgoing_teacher_name AS OutgoingTeacherName,
                    th.is_critical AS IsCritical, th.summary_notes AS SummaryNotes,
                    th.outstanding_care_tasks AS OutstandingTasks,
                    th.created_at AS CreatedAt
                FROM teacher_handovers th
                JOIN classrooms cl ON th.classroom_id = cl.id
                JOIN branches b ON th.branch_id = b.id
                WHERE th.tenant_id = @TenantId
                  AND b.organization_id = @OrgId
                  AND th.status = 'PendingAcknowledgement'
                  AND (@BranchId IS NULL OR th.branch_id = @BranchId)
                  AND (@ClassroomId IS NULL OR th.classroom_id = @ClassroomId)";

            var pendingHandovers = (await conn.QueryAsync<dynamic>(handoversSql, new
            {
                TenantId = tenantId,
                OrgId = orgId.Value,
                BranchId = filterBranchId,
                ClassroomId = filterClassroomId
            })).ToList();

            foreach (var ho in pendingHandovers)
            {
                DateTime createdAt = Convert.ToDateTime(ho.createdat);
                bool isCritical = Convert.ToBoolean(ho.iscritical);
                bool isOverdue = isCritical || (DateTime.UtcNow - createdAt).TotalMinutes > 30;

                model.Alerts.Add(new OperationalAlertItem
                {
                    AlertType = "Staffing",
                    Severity = isOverdue ? "High" : "Medium",
                    Title = $"Shift Handover Pending: {ho.classroomname} ({ho.shifttype})",
                    Description = $"{ho.outgoingteachername} submitted shift handover. Outstanding tasks: {ho.outstandingtasks ?? ho.summarynotes ?? "Awaiting incoming acknowledgement"}.",
                    ActionUrl = "/daily-care",
                    ActionText = "Acknowledge Handover",
                    SourceContext = ho.classroomname
                });
            }
        }
        catch (Exception ex)
        {
            // Table may be migrating or empty
            Console.WriteLine($"Handover query notice: {ex.Message}");
        }

        // 11. Generate Actionable Alerts
        GenerateActionableAlerts(model);

        // 12. Generate Classroom Readiness Checklist
        GenerateReadinessChecklist(model);

        return model;
    }

    private string ResolveEffectiveRole(string? overrideRole)
    {
        if (!string.IsNullOrWhiteSpace(overrideRole))
        {
            return overrideRole;
        }

        if (_userContext.Roles.Contains(RoleTemplates.PlatformAdmin, StringComparer.OrdinalIgnoreCase))
            return RoleTemplates.PlatformAdmin;

        if (_userContext.Roles.Contains(RoleTemplates.OrganizationOwner, StringComparer.OrdinalIgnoreCase) ||
            _userContext.Roles.Contains(RoleTemplates.OrganizationAdmin, StringComparer.OrdinalIgnoreCase) ||
            _userContext.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            return RoleTemplates.OrganizationAdmin;

        if (_userContext.Roles.Contains(RoleTemplates.BranchAdmin, StringComparer.OrdinalIgnoreCase))
            return RoleTemplates.BranchAdmin;

        if (_userContext.Roles.Contains(RoleTemplates.DaycareTeacher, StringComparer.OrdinalIgnoreCase) ||
            _userContext.Roles.Contains(RoleTemplates.ClassroomTeacher, StringComparer.OrdinalIgnoreCase) ||
            _userContext.Roles.Contains("Teacher", StringComparer.OrdinalIgnoreCase))
            return RoleTemplates.ClassroomTeacher;

        return RoleTemplates.OrganizationAdmin;
    }

    private void GenerateActionableAlerts(OperationsBoardModel model)
    {
        // 1. Capacity Warnings
        foreach (var cr in model.Classrooms.Where(c => c.IsOverCapacity))
        {
            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "Capacity",
                Severity = "High",
                Title = $"{cr.ClassroomName} exceeds room capacity",
                Description = $"{cr.PresentChildren} children present for max licensed capacity of {cr.Capacity} ({cr.BranchName}).",
                ActionUrl = $"/classrooms",
                ActionText = "Adjust Placement",
                SourceContext = cr.ClassroomName
            });
        }

        // 2. Staffing Coverage Risks
        foreach (var cr in model.Classrooms.Where(c => c.IsUnderstaffed))
        {
            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "Staffing",
                Severity = "High",
                Title = $"Staffing ratio risk in {cr.ClassroomName}",
                Description = $"{cr.PresentStaff} teachers on site for {cr.PresentChildren} children (requires {cr.RequiredStaff} staff under {cr.AgeGroup} policy).",
                ActionUrl = $"/teachers",
                ActionText = "Assign Staff",
                SourceContext = cr.ClassroomName
            });
        }

        // 3. Missing Care Records
        if (model.MissingCareRecordsCount > 0)
        {
            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "CareRecord",
                Severity = "Medium",
                Title = $"{model.MissingCareRecordsCount} checked-in children have no care events today",
                Description = "Routine care observations (meals, naps, diaper/toileting) have not yet been logged for these children.",
                ActionUrl = "/daily-care",
                ActionText = "Log Daily Care",
                SourceContext = "Care Records"
            });
        }

        // 4. Unattended Check-ins (Expected not arrived)
        if (model.ExpectedNotArrivedCount > 0 && model.BoardDate.Date == DateTime.Today)
        {
            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "Attendance",
                Severity = "Info",
                Title = $"{model.ExpectedNotArrivedCount} children expected but not yet checked in",
                Description = "Review absent notifications or confirm late arrivals in attendance overview.",
                ActionUrl = "/attendance",
                ActionText = "Check Attendance",
                SourceContext = "Arrivals"
            });
        }

        // 5. Late Pickups
        if (model.LatePickupsCount > 0)
        {
            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "Pickup",
                Severity = "High",
                Title = $"{model.LatePickupsCount} children remain on site near/past closing",
                Description = "Ensure pickup guardians have been contacted and emergency contacts are on standby.",
                ActionUrl = "/attendance",
                ActionText = "Verify Release",
                SourceContext = "Closing Release"
            });
        }

        // 6. Pending Enrollments
        if (model.PendingEnrollmentsCount > 0 && (model.EffectiveRole == RoleTemplates.OrganizationAdmin || model.EffectiveRole == RoleTemplates.BranchAdmin))
        {
            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "Enrollment",
                Severity = "Info",
                Title = $"{model.PendingEnrollmentsCount} pending child registration drafts",
                Description = "Incomplete intake profiles require review, emergency contact verification, or classroom placement.",
                ActionUrl = "/children",
                ActionText = "Review Intake",
                SourceContext = "Enrollment"
            });
        }

        // 7. Financial Overdue Invoices (finance users only)
        if (model.HasFinanceAccess && model.OverdueInvoicesCount > 0)
        {
            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "Finance",
                Severity = "Medium",
                Title = $"{model.OverdueInvoicesCount} overdue organization invoices (₹{model.OutstandingReceivables:N0})",
                Description = "Overdue subscription or service invoices require reconciliation in the Account Book.",
                ActionUrl = "/billing",
                ActionText = "Open Billing",
                SourceContext = "Finance"
            });
        }
    }

    private void GenerateReadinessChecklist(OperationsBoardModel model)
    {
        model.ReadinessChecklist = new List<ReadinessChecklistItem>
        {
            new ReadinessChecklistItem
            {
                Id = "check-kiosk",
                Label = "Arrival Sign-In Active",
                Description = "Attendance kiosk and parent check-in station verified online.",
                IsCompleted = model.CheckedInChildrenCount > 0,
                Category = "Opening"
            },
            new ReadinessChecklistItem
            {
                Id = "check-ratios",
                Label = "Classroom Ratios Met",
                Description = "Every operating classroom meets staff-to-child ratio requirements.",
                IsCompleted = model.UnderstaffedClassroomsCount == 0 && model.PresentStaffCount > 0,
                Category = "Safety"
            },
            new ReadinessChecklistItem
            {
                Id = "check-experiences",
                Label = "Today's Materials Prepared",
                Description = "Materials and learning zones set up for today's scheduled experiences.",
                IsCompleted = model.TodayRhythm.Any(r => r.Status == "InProgress" || r.Status == "Completed"),
                Category = "Material"
            },
            new ReadinessChecklistItem
            {
                Id = "check-care-sync",
                Label = "Care Timeline Initiated",
                Description = "First morning comfort check, snack, and hydration logged.",
                IsCompleted = model.MissingCareRecordsCount < model.CheckedInChildrenCount && model.CheckedInChildrenCount > 0,
                Category = "Care"
            }
        };
    }
}
