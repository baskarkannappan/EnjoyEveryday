using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
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
    private readonly IDaycareOperationsBoardRepository _boardRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAccessScopeRepository _accessScopeRepo;
    private readonly TimeProvider _timeProvider;

    public DaycareOperationsBoardService(
        IDaycareOperationsBoardRepository boardRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAccessScopeRepository accessScopeRepo,
        TimeProvider timeProvider)
    {
        _boardRepository = boardRepository ?? throw new ArgumentNullException(nameof(boardRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _accessScopeRepo = accessScopeRepo ?? throw new ArgumentNullException(nameof(accessScopeRepo));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OperationsBoardModel> GetOperationsBoardAsync(OperationsBoardFilter filter, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _timeProvider.GetLocalNow();
        var date = filter.Date?.Date ?? now.Date;

        // 1. Resolve Organization
        var orgId = filter.OrganizationId ?? _userContext.OrganizationId;
        if (orgId == null || orgId == Guid.Empty)
        {
            orgId = await _boardRepository.GetDefaultOrganizationIdAsync(tenantId, ct);
        }

        var orgName = (orgId.HasValue ? await _boardRepository.GetOrganizationNameAsync(tenantId, orgId.Value, ct) : null) ?? "EnjoyEveryday Daycare";

        // 2. Resolve Role Scope
        var effectiveRole = ResolveEffectiveRole(filter.RoleOverride);

        // Get user branch/classroom assignments
        var assignedBranchIds = orgId.HasValue
            ? (await _accessScopeRepo.GetAssignedBranchIdsAsync(tenantId, orgId.Value, _userContext.UserId, ct)).ToList()
            : new List<Guid>();
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
            OrganizationId = orgId ?? Guid.Empty,
            OrganizationName = orgName,
            SelectedBranchId = filterBranchId,
            SelectedClassroomId = filterClassroomId
        };

        if (!orgId.HasValue || orgId.Value == Guid.Empty)
        {
            return model;
        }

        // 3. Classrooms & Occupancy Query
        var classroomRecords = await _boardRepository.GetClassroomsAsync(tenantId, orgId.Value, date, filterBranchId, filterClassroomId, ct);

        var classroomRows = classroomRecords.Select(cl => new ClassroomBoardItem
        {
            ClassroomId = cl.ClassroomId,
            ClassroomName = cl.ClassroomName,
            AgeGroup = cl.AgeGroup,
            BranchId = cl.BranchId,
            BranchName = cl.BranchName,
            Capacity = cl.Capacity,
            EnrolledChildren = cl.EnrolledChildren,
            PresentChildren = cl.PresentChildren,
            ExpectedChildren = cl.ExpectedChildren,
            PresentStaff = cl.PresentStaff
        }).ToList();

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
        var attSummary = await _boardRepository.GetChildAttendanceSummaryAsync(tenantId, orgId.Value, date, filterBranchId, filterClassroomId, ct);

        int checkedIn = attSummary.CheckedIn;
        int checkedOut = attSummary.CheckedOut;
        int absent = attSummary.Absent;

        int totalExpected = classroomRows.Sum(c => c.ExpectedChildren);
        int expectedNotArrived = Math.Max(0, totalExpected - checkedIn - absent);

        model.ExpectedChildrenCount = totalExpected;
        model.CheckedInChildrenCount = checkedIn;
        model.CheckedOutChildrenCount = checkedOut;
        model.AbsentChildrenCount = absent;
        model.ExpectedNotArrivedCount = expectedNotArrived;

        // 5. Staff Summary Query
        var staffSummary = await _boardRepository.GetStaffSummaryAsync(tenantId, orgId.Value, date, ct);

        model.ExpectedStaffCount = staffSummary.ExpectedStaff;
        model.PresentStaffCount = staffSummary.PresentStaff;
        model.AbsentStaffCount = staffSummary.AbsentStaff;

        // 6. Experience & Rhythm Schedules Query
        var scheduleRecords = await _boardRepository.GetExperienceSchedulesAsync(tenantId, orgId.Value, date, filterBranchId, filterClassroomId, ct);
        var schedules = scheduleRecords.Select(es => new RhythmScheduleItem
        {
            Id = es.Id,
            IsCelebration = false,
            Title = es.Title,
            TimeDisplay = es.TimeDisplay,
            ScopeDisplay = es.ScopeDisplay,
            Status = es.Status,
            ClassroomId = es.ClassroomId,
            ClassroomName = es.ClassroomName,
            PreparationNotes = es.PreparationNotes
        }).ToList();

        // Add today's celebrations/events
        var eventRecords = await _boardRepository.GetEventsAsync(tenantId, date, ct);
        var eventRows = eventRecords.Select(ev => new RhythmScheduleItem
        {
            Id = ev.Id,
            IsCelebration = true,
            Title = ev.Title,
            TimeDisplay = ev.TimeDisplay,
            ScopeDisplay = ev.ScopeDisplay,
            Status = ev.Status,
            PreparationNotes = ev.PreparationNotes
        }).ToList();

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
        var careRecords = await _boardRepository.GetTeacherChildrenCareStatusAsync(tenantId, orgId.Value, date, date.Date, date.Date.AddDays(1).AddTicks(-1), filterBranchId, filterClassroomId, ct);
        var careList = careRecords.Select(c => new TeacherChildCareItem
        {
            ChildId = c.ChildId,
            FullName = c.FullName,
            PhotoUrl = c.PhotoUrl,
            IsCheckedIn = c.IsCheckedIn,
            ArrivalTime = c.ArrivalTime,
            CareEventsCount = c.CareEventsCount,
            LastRecordedCareType = c.LastRecordedCareType,
            LastRecordedTime = c.LastRecordedTime
        }).ToList();

        model.TeacherChildrenCareStatus = careList;
        model.MissingCareRecordsCount = careList.Count(c => c.NeedsAttention);

        // 8. Late Pickups Query (children present past 17:00 or departure time passed)
        if (now.TimeOfDay > new TimeSpan(16, 30, 0))
        {
            model.LatePickupsCount = careList.Count(c => c.IsCheckedIn);
        }

        // 9. Pending Enrollments Query
        model.PendingEnrollmentsCount = await _boardRepository.GetPendingEnrollmentsCountAsync(tenantId, orgId.Value, ct);

        // 10. Financial Summary (Strictly permission-gated)
        if (_userContext.HasPermission(Permissions.OrganizationBillingView) || _userContext.HasPermission(Permissions.PlatformBillingManage))
        {
            model.HasFinanceAccess = true;
            var finSummary = await _boardRepository.GetFinancialSummaryAsync(tenantId, orgId.Value, ct);
            model.OutstandingReceivables = finSummary.Outstanding;
            model.OverdueInvoicesCount = finSummary.OverdueCount;
        }

        // 10b. Teacher Handovers Query
        var pendingHandovers = await _boardRepository.GetPendingTeacherHandoversAsync(tenantId, orgId.Value, filterBranchId, filterClassroomId, ct);
        foreach (var ho in pendingHandovers)
        {
            bool isOverdue = ho.IsCritical || (DateTime.UtcNow - ho.CreatedAt).TotalMinutes > 30;

            model.Alerts.Add(new OperationalAlertItem
            {
                AlertType = "Staffing",
                Severity = isOverdue ? "High" : "Medium",
                Title = $"Shift Handover Pending: {ho.ClassroomName} ({ho.ShiftType})",
                Description = $"{ho.OutgoingTeacherName} submitted shift handover. Outstanding tasks: {ho.OutstandingTasks ?? ho.SummaryNotes ?? "Awaiting incoming acknowledgement"}.",
                ActionUrl = "/daily-care",
                ActionText = "Acknowledge Handover",
                SourceContext = ho.ClassroomName
            });
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
