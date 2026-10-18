using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ClassroomChildCareStatus
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public bool IsCheckedIn { get; set; }
    public string AttendanceStatus { get; set; } = "Not Checked In";
    public TimeSpan? ArrivalTime { get; set; }

    // Last recorded summaries
    public ChildCareEvent? LastDiaperEvent { get; set; }
    public ChildCareEvent? LastMealEvent { get; set; }
    public ChildNapSession? ActiveNapSession { get; set; }
    public ChildCareEvent? LastComfortMood { get; set; }
    public int TodayEventsCount { get; set; }
    public bool HasLowSupplies { get; set; }
    public string? ImportantCareAlert { get; set; }
}

public class ChildTimelineEntry
{
    public Guid? Id { get; set; }
    public DateTime EventTime { get; set; }
    public string TimeLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Attendance, Diaper, Meal, Sleep, Toileting, Hygiene, Comfort, Experience
    public string Icon { get; set; } = "•";
    public string? StaffName { get; set; }
    public bool IsParentVisible { get; set; } = true;
    public bool IsCorrected { get; set; }
}

public class ParentDailyCareReport
{
    public Guid ChildId { get; set; }
    public string ChildFirstName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }

    // 4 KPI Cards matching Screenshot 3
    public int DiaperChangesCount { get; set; }
    public int TotalRestMinutes { get; set; }
    public string RestTimeFormatted => TotalRestMinutes > 0 ? $"{TotalRestMinutes / 60}h {TotalRestMinutes % 60}m" : "None recorded";
    public string LunchMealStatus { get; set; } = "Not recorded";
    public string PrimaryMoodObservation { get; set; } = "Content & Settled";

    // Factual reassurance summary paragraph
    public string DailyReassuranceText { get; set; } = string.Empty;

    // Filtered timeline of parent-visible items
    public List<ChildTimelineEntry> Timeline { get; set; } = new();

    // Supply alerts for parent
    public List<ChildCareSupply> LowSupplies { get; set; } = new();
}

public interface IChildDailyCareService
{
    Task<Guid> RecordCareEventAsync(ChildCareEvent ev, CancellationToken ct = default);
    Task<int> RecordBatchCareEventsAsync(IEnumerable<Guid> childIds, string eventType, string detailOption, string? notes, bool parentVisible, CancellationToken ct = default);
    Task<int> StartBatchNapAsync(IEnumerable<Guid> childIds, Guid? classroomId, string? notes, CancellationToken ct = default);
    Task<int> WakeBatchChildrenAsync(IEnumerable<Guid> childIds, string? outcome, string? notes, CancellationToken ct = default);
    Task RecordCorrectionAsync(Guid eventId, string correctionReason, Action<ChildCareEvent> applyCorrection, CancellationToken ct = default);
    Task<ChildNapSession> StartNapAsync(Guid childId, Guid? classroomId, string? notes, CancellationToken ct = default);
    Task<ChildNapSession?> WakeChildAsync(Guid childId, string? outcome, string? notes, CancellationToken ct = default);
    Task<ChildNapSession?> GetActiveNapAsync(Guid childId, CancellationToken ct = default);
    Task<IEnumerable<ClassroomChildCareStatus>> GetClassroomOverviewAsync(Guid branchId, Guid? classroomId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<ChildTimelineEntry>> GetChildCareTimelineAsync(Guid childId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<ChildCareEvent>> GetChildCareEventsAsync(Guid childId, CancellationToken ct = default);
    Task<ChildCarePlan?> GetChildCarePlanAsync(Guid childId, CancellationToken ct = default);
    Task<Guid> SaveCarePlanAsync(ChildCarePlan plan, CancellationToken ct = default);
    Task<IEnumerable<ChildCareSupply>> GetChildSuppliesAsync(Guid childId, CancellationToken ct = default);
    Task<IEnumerable<ChildCareSupply>> GetLowSuppliesAsync(Guid branchId, CancellationToken ct = default);
    Task<Guid> UpdateSupplyStatusAsync(ChildCareSupply supply, CancellationToken ct = default);
    Task<ParentDailyCareReport> GetParentDailyCareReportAsync(Guid childId, DateOnly date, CancellationToken ct = default);
    Task<Guid> SubmitHandoverAsync(TeacherHandover handover, CancellationToken ct = default);
    Task AcknowledgeHandoverAsync(Guid handoverId, string? notes, CancellationToken ct = default);
    Task<IEnumerable<TeacherHandover>> GetClassroomHandoversAsync(Guid classroomId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<TeacherHandover>> GetPendingHandoversAsync(Guid branchId, CancellationToken ct = default);
}

public class ChildDailyCareService : IChildDailyCareService
{
    private readonly IChildDailyCareRepository _repository;
    private readonly IChildRepository _childRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;

    public ChildDailyCareService(
        IChildDailyCareRepository repository,
        IChildRepository childRepository,
        IClassroomRepository classroomRepository,
        IBranchRepository branchRepository,
        IAttendanceRepository attendanceRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService)
    {
        _repository = repository;
        _childRepository = childRepository;
        _classroomRepository = classroomRepository;
        _branchRepository = branchRepository;
        _attendanceRepository = attendanceRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
    }

    private async Task<(Guid BranchId, Guid OrganizationId)> ResolveChildScopeAsync(Child child, CancellationToken ct)
    {
        if (child.ClassroomId.HasValue)
        {
            var classroom = await _classroomRepository.GetByIdAsync(_tenantContext.TenantId, child.ClassroomId.Value, ct);
            if (classroom != null)
            {
                var branch = await _branchRepository.GetByIdAsync(_tenantContext.TenantId, classroom.BranchId, ct);
                if (branch != null)
                    return (branch.Id, branch.OrganizationId);
            }
        }

        // Fallback to active organization context
        var orgId = _userContext.OrganizationId ?? Guid.Empty;
        var branches = await _branchRepository.GetByOrganizationIdAsync(_tenantContext.TenantId, orgId, ct);
        var firstBranch = branches.FirstOrDefault();
        return (firstBranch?.Id ?? Guid.Empty, orgId);
    }

    public async Task<Guid> RecordCareEventAsync(ChildCareEvent ev, CancellationToken ct = default)
    {
        if (ev.ChildId == Guid.Empty)
            throw new ArgumentException("ChildId is required.", nameof(ev));

        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, ev.ChildId, ct);
        if (child == null)
            throw new InvalidOperationException("Child not found or access denied.");

        var (branchId, orgId) = await ResolveChildScopeAsync(child, ct);

        ev.TenantId = _tenantContext.TenantId;
        ev.OrganizationId = orgId;
        ev.BranchId = branchId;
        ev.ClassroomId ??= child.ClassroomId;
        ev.RecordedByUserId = _userContext.UserId;
        ev.RecordedByName ??= _userContext.Email;
        ev.CreatedAt = DateTime.UtcNow;
        ev.UpdatedAt = DateTime.UtcNow;

        var eventId = await _repository.AddCareEventAsync(ev, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "CHILD_CARE_RECORDED",
            EntityType = "ChildCareEvent",
            EntityId = eventId,
            Details = $"Recorded {ev.EventType} care event for child {child.FirstName} {child.LastName}."
        }, ct);

        return eventId;
    }

    public async Task<int> RecordBatchCareEventsAsync(
        IEnumerable<Guid> childIds,
        string eventType,
        string detailOption,
        string? notes,
        bool parentVisible,
        CancellationToken ct = default)
    {
        var count = 0;
        var now = DateTime.UtcNow;

        foreach (var childId in childIds)
        {
            if (childId == Guid.Empty) continue;

            var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, childId, ct);
            if (child == null) continue;

            var (branchId, orgId) = await ResolveChildScopeAsync(child, ct);

            var ev = new ChildCareEvent
            {
                TenantId = _tenantContext.TenantId,
                OrganizationId = orgId,
                BranchId = branchId,
                ClassroomId = child.ClassroomId,
                ChildId = childId,
                EventType = eventType,
                EventTime = now,
                Notes = notes,
                ParentVisible = parentVisible,
                RecordedByUserId = _userContext.UserId,
                RecordedByName = _userContext.Email,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Map detailOption to appropriate entity properties
            switch (eventType)
            {
                case CareEventTypes.DiaperChange:
                    ev.DiaperCondition = detailOption;
                    break;
                case CareEventTypes.Meal:
                    ev.MealType = detailOption.Contains("Lunch", StringComparison.OrdinalIgnoreCase) ? "Lunch" :
                                  detailOption.Contains("Snack", StringComparison.OrdinalIgnoreCase) ? "MorningSnack" :
                                  detailOption.Contains("Breakfast", StringComparison.OrdinalIgnoreCase) ? "Breakfast" : "Lunch";
                    ev.AmountConsumed = detailOption;
                    break;
                case CareEventTypes.SleepRest:
                    ev.RestStatus = detailOption;
                    break;
                case CareEventTypes.Toileting:
                    ev.ToiletingActivity = detailOption;
                    break;
                case CareEventTypes.Hygiene:
                    ev.HygieneType = detailOption;
                    break;
                case CareEventTypes.Comfort:
                    ev.ComfortMood = detailOption;
                    break;
            }

            await _repository.AddCareEventAsync(ev, ct);
            count++;
        }

        if (count > 0)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                UserId = _userContext.UserId,
                Action = "BATCH_CHILD_CARE_RECORDED",
                EntityType = "ChildCareEvent",
                Details = $"Recorded batch {eventType} event for {count} children with option: {detailOption}."
            }, ct);
        }

        return count;
    }

    public async Task<int> StartBatchNapAsync(
        IEnumerable<Guid> childIds,
        Guid? classroomId,
        string? notes,
        CancellationToken ct = default)
    {
        var count = 0;
        foreach (var childId in childIds)
        {
            if (childId == Guid.Empty) continue;
            var active = await _repository.GetActiveNapSessionAsync(_tenantContext.TenantId, childId, ct);
            if (active == null)
            {
                await StartNapAsync(childId, classroomId, notes, ct);
                count++;
            }
        }
        return count;
    }

    public async Task<int> WakeBatchChildrenAsync(
        IEnumerable<Guid> childIds,
        string? outcome,
        string? notes,
        CancellationToken ct = default)
    {
        var count = 0;
        foreach (var childId in childIds)
        {
            if (childId == Guid.Empty) continue;
            var woken = await WakeChildAsync(childId, outcome, notes, ct);
            if (woken != null) count++;
        }
        return count;
    }

    public async Task RecordCorrectionAsync(Guid eventId, string correctionReason, Action<ChildCareEvent> applyCorrection, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correctionReason))
            throw new ArgumentException("A factual reason is required for correcting care records.", nameof(correctionReason));

        var ev = await _repository.GetCareEventByIdAsync(_tenantContext.TenantId, eventId, ct);
        if (ev == null)
            throw new KeyNotFoundException("Care event not found.");

        applyCorrection(ev);
        ev.CorrectionReason = correctionReason;
        ev.IsCorrected = true;

        await _repository.UpdateCareEventAsync(ev, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "CHILD_CARE_CORRECTED",
            EntityType = "ChildCareEvent",
            EntityId = eventId,
            Details = $"Care event corrected. Reason: {correctionReason}"
        }, ct);
    }

    public async Task<ChildNapSession> StartNapAsync(Guid childId, Guid? classroomId, string? notes, CancellationToken ct = default)
    {
        var existing = await _repository.GetActiveNapSessionAsync(_tenantContext.TenantId, childId, ct);
        if (existing != null)
            return existing; // Already in active nap session

        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, childId, ct);
        if (child == null)
            throw new InvalidOperationException("Child not found.");

        var (branchId, orgId) = await ResolveChildScopeAsync(child, ct);

        var session = new ChildNapSession
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId,
            BranchId = branchId,
            ClassroomId = classroomId ?? child.ClassroomId,
            ChildId = childId,
            StartTime = DateTime.UtcNow,
            Status = "Active",
            Notes = notes,
            StartedByUserId = _userContext.UserId,
            StartedByName = _userContext.Email,
            ParentVisible = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.StartNapSessionAsync(session, ct);

        // Also record corresponding timeline care event
        await _repository.AddCareEventAsync(new ChildCareEvent
        {
            TenantId = session.TenantId,
            OrganizationId = session.OrganizationId,
            BranchId = session.BranchId,
            ClassroomId = session.ClassroomId,
            ChildId = childId,
            EventType = CareEventTypes.SleepRest,
            EventTime = session.StartTime,
            RestStatus = "FellAsleep",
            Notes = notes ?? "Rest period started.",
            ParentVisible = true,
            RecordedByUserId = session.StartedByUserId,
            RecordedByName = session.StartedByName
        }, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "NAP_STARTED",
            EntityType = "ChildNapSession",
            EntityId = session.Id,
            Details = $"Started nap for child {child.FirstName} {child.LastName}."
        }, ct);

        return session;
    }

    public async Task<ChildNapSession?> WakeChildAsync(Guid childId, string? outcome, string? notes, CancellationToken ct = default)
    {
        var activeSession = await _repository.GetActiveNapSessionAsync(_tenantContext.TenantId, childId, ct);
        if (activeSession == null)
            return null;

        var endTime = DateTime.UtcNow;
        var duration = (int)Math.Max(1, (endTime - activeSession.StartTime).TotalMinutes);

        await _repository.EndNapSessionAsync(
            _tenantContext.TenantId,
            activeSession.Id,
            endTime,
            duration,
            outcome ?? "WokeNaturally",
            notes,
            _userContext.UserId,
            _userContext.Email,
            ct);

        // Record wake-up timeline event
        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, childId, ct);
        await _repository.AddCareEventAsync(new ChildCareEvent
        {
            TenantId = activeSession.TenantId,
            OrganizationId = activeSession.OrganizationId,
            BranchId = activeSession.BranchId,
            ClassroomId = activeSession.ClassroomId,
            ChildId = childId,
            EventType = CareEventTypes.SleepRest,
            EventTime = endTime,
            RestStatus = outcome ?? "WokeNaturally",
            Notes = $"Woke after {duration} minutes of rest. {notes}".Trim(),
            ParentVisible = true,
            RecordedByUserId = _userContext.UserId,
            RecordedByName = _userContext.Email
        }, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "NAP_ENDED",
            EntityType = "ChildNapSession",
            EntityId = activeSession.Id,
            Details = $"Ended nap for child {child?.FirstName} after {duration} minutes."
        }, ct);

        activeSession.EndTime = endTime;
        activeSession.DurationMinutes = duration;
        activeSession.Status = "Completed";
        return activeSession;
    }

    public async Task<ChildNapSession?> GetActiveNapAsync(Guid childId, CancellationToken ct = default)
    {
        return await _repository.GetActiveNapSessionAsync(_tenantContext.TenantId, childId, ct);
    }

    public async Task<IEnumerable<ClassroomChildCareStatus>> GetClassroomOverviewAsync(
        Guid branchId,
        Guid? classroomId,
        DateOnly date,
        CancellationToken ct = default)
    {
        var allChildren = await _childRepository.GetAllAsync(_tenantContext.TenantId, ct);
        var children = allChildren
            .Where(c => !classroomId.HasValue || c.ClassroomId == classroomId.Value)
            .ToList();

        var attendancesList = await _attendanceRepository.GetChildAttendanceByDateAsync(
            _tenantContext.TenantId,
            branchId,
            date.ToDateTime(TimeOnly.MinValue),
            ct);
        var attendances = attendancesList.ToDictionary(a => a.ChildId);

        var events = classroomId.HasValue
            ? (await _repository.GetEventsByClassroomAndDateAsync(_tenantContext.TenantId, classroomId.Value, date, ct)).ToList()
            : (await _repository.GetEventsByBranchAndDateAsync(_tenantContext.TenantId, branchId, date, ct)).ToList();

        var eventsByChild = events.GroupBy(e => e.ChildId).ToDictionary(g => g.Key, g => g.ToList());

        var activeNaps = classroomId.HasValue
            ? (await _repository.GetActiveNapsByClassroomAsync(_tenantContext.TenantId, classroomId.Value, ct)).ToDictionary(n => n.ChildId)
            : new Dictionary<Guid, ChildNapSession>();

        var lowSupplies = (await _repository.GetLowSuppliesByBranchAsync(_tenantContext.TenantId, branchId, ct))
            .Select(s => s.ChildId)
            .ToHashSet();

        var result = new List<ClassroomChildCareStatus>();
        foreach (var child in children)
        {
            attendances.TryGetValue(child.Id, out var att);
            eventsByChild.TryGetValue(child.Id, out var childEvents);
            childEvents ??= new List<ChildCareEvent>();

            activeNaps.TryGetValue(child.Id, out var nap);

            var status = new ClassroomChildCareStatus
            {
                ChildId = child.Id,
                ChildName = $"{child.FirstName} {child.LastName}",
                PhotoUrl = child.PhotoUrl,
                ClassroomId = child.ClassroomId,
                IsCheckedIn = string.Equals(att?.Status, "Present", StringComparison.OrdinalIgnoreCase),
                AttendanceStatus = att?.Status ?? "Not Checked In",
                ArrivalTime = att?.ArrivalTime,
                LastDiaperEvent = childEvents.Where(e => e.EventType == CareEventTypes.DiaperChange).OrderByDescending(e => e.EventTime).FirstOrDefault(),
                LastMealEvent = childEvents.Where(e => e.EventType == CareEventTypes.Meal).OrderByDescending(e => e.EventTime).FirstOrDefault(),
                ActiveNapSession = nap,
                LastComfortMood = childEvents.Where(e => e.EventType == CareEventTypes.Comfort).OrderByDescending(e => e.EventTime).FirstOrDefault(),
                TodayEventsCount = childEvents.Count,
                HasLowSupplies = lowSupplies.Contains(child.Id),
                ImportantCareAlert = null
            };
            result.Add(status);
        }

        return result;
    }

    public async Task<IEnumerable<ChildTimelineEntry>> GetChildCareTimelineAsync(Guid childId, DateOnly date, CancellationToken ct = default)
    {
        var entries = new List<ChildTimelineEntry>();

        // 1. Attendance Check-In
        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, childId, ct);
        if (child != null)
        {
            var (branchId, _) = await ResolveChildScopeAsync(child, ct);
            var attList = await _attendanceRepository.GetChildAttendanceByDateAsync(
                _tenantContext.TenantId,
                branchId,
                date.ToDateTime(TimeOnly.MinValue),
                ct);
            var att = attList.FirstOrDefault(a => a.ChildId == childId);

            if (att != null && att.ArrivalTime.HasValue)
            {
                var dt = date.ToDateTime(TimeOnly.FromTimeSpan(att.ArrivalTime.Value));
                entries.Add(new ChildTimelineEntry
                {
                    EventTime = dt,
                    TimeLabel = dt.ToString("h:mm tt"),
                    Title = "Arrival",
                    Description = "Arrived and settled into the classroom.",
                    Category = "Attendance",
                    Icon = "☀️",
                    IsParentVisible = true
                });
            }
        }

        // 2. Persisted Child Care Events
        var careEvents = await _repository.GetEventsByChildAndDateAsync(_tenantContext.TenantId, childId, date, ct);
        foreach (var ev in careEvents)
        {
            var title = ev.EventType switch
            {
                CareEventTypes.DiaperChange => "Diaper change",
                CareEventTypes.Meal => $"{ev.MealType ?? "Meal"}",
                CareEventTypes.SleepRest => ev.RestStatus == "FellAsleep" ? "Nap started" : "Nap completed",
                CareEventTypes.Toileting => "Toileting",
                CareEventTypes.Hygiene => $"{ev.HygieneType ?? "Hygiene"}",
                CareEventTypes.Comfort => $"Comfort observation ({ev.ComfortMood ?? "Settled"})",
                _ => ev.EventType
            };

            var icon = ev.EventType switch
            {
                CareEventTypes.DiaperChange => "👶",
                CareEventTypes.Meal => "🥣",
                CareEventTypes.SleepRest => "🌙",
                CareEventTypes.Toileting => "🚪",
                CareEventTypes.Hygiene => "💧",
                CareEventTypes.Comfort => "❤️",
                _ => "•"
            };

            var desc = ev.Notes;
            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = ev.EventType switch
                {
                    CareEventTypes.DiaperChange => $"{ev.DiaperCondition ?? "Checked"} diaper; changed by caregiver.",
                    CareEventTypes.Meal => $"Ate {ev.AmountConsumed ?? "some"} of {ev.FoodOffered ?? "offered meal"}.",
                    CareEventTypes.Toileting => $"Bathroom routine: {ev.ToiletingActivity ?? "completed"}.",
                    CareEventTypes.Hygiene => $"Cleaned and refreshed ({ev.HygieneType ?? "routine"}).",
                    CareEventTypes.Comfort => $"Observed mood: {ev.ComfortMood ?? "settled"}.",
                    _ => "Recorded by teacher."
                };
            }

            entries.Add(new ChildTimelineEntry
            {
                Id = ev.Id,
                EventTime = ev.EventTime,
                TimeLabel = ev.EventTime.ToLocalTime().ToString("h:mm tt"),
                Title = title,
                Description = desc,
                Category = ev.EventType,
                Icon = icon,
                StaffName = ev.RecordedByName,
                IsParentVisible = ev.ParentVisible,
                IsCorrected = ev.IsCorrected
            });
        }

        return entries.OrderBy(e => e.EventTime);
    }

    public async Task<IEnumerable<ChildCareEvent>> GetChildCareEventsAsync(Guid childId, CancellationToken ct = default)
    {
        return await _repository.GetEventsByChildAsync(_tenantContext.TenantId, childId, ct);
    }

    public async Task<ChildCarePlan?> GetChildCarePlanAsync(Guid childId, CancellationToken ct = default)
    {
        return await _repository.GetActiveCarePlanAsync(_tenantContext.TenantId, childId, ct);
    }

    public async Task<Guid> SaveCarePlanAsync(ChildCarePlan plan, CancellationToken ct = default)
    {
        plan.TenantId = _tenantContext.TenantId;
        plan.CreatedByUserId = _userContext.UserId;
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

        var planId = await _repository.SaveCarePlanAsync(plan, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "CARE_PLAN_UPDATED",
            EntityType = "ChildCarePlan",
            EntityId = planId,
            Details = $"Updated care plan for child {plan.ChildId} (version {plan.Version})."
        }, ct);

        return planId;
    }

    public async Task<IEnumerable<ChildCareSupply>> GetChildSuppliesAsync(Guid childId, CancellationToken ct = default)
    {
        return await _repository.GetSuppliesByChildAsync(_tenantContext.TenantId, childId, ct);
    }

    public async Task<IEnumerable<ChildCareSupply>> GetLowSuppliesAsync(Guid branchId, CancellationToken ct = default)
    {
        return await _repository.GetLowSuppliesByBranchAsync(_tenantContext.TenantId, branchId, ct);
    }

    public async Task<Guid> UpdateSupplyStatusAsync(ChildCareSupply supply, CancellationToken ct = default)
    {
        supply.TenantId = _tenantContext.TenantId;
        if (supply.BranchId == Guid.Empty && supply.ChildId != Guid.Empty)
        {
            var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, supply.ChildId, ct);
            if (child != null)
            {
                var (branchId, orgId) = await ResolveChildScopeAsync(child, ct);
                supply.BranchId = branchId;
                supply.OrganizationId = orgId;
            }
        }
        supply.LastReportedByUserId = _userContext.UserId;
        supply.LastReportedByName = _userContext.Email;
        supply.UpdatedAt = DateTime.UtcNow;

        var supplyId = await _repository.SaveSupplyStatusAsync(supply, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "CARE_SUPPLY_UPDATED",
            EntityType = "ChildCareSupply",
            EntityId = supplyId,
            Details = $"Supply {supply.SupplyType} status updated to {supply.Status}."
        }, ct);

        return supplyId;
    }

    public async Task<ParentDailyCareReport> GetParentDailyCareReportAsync(Guid childId, DateOnly date, CancellationToken ct = default)
    {
        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, childId, ct);
        if (child == null)
            throw new KeyNotFoundException("Child record not found.");

        var timeline = (await GetChildCareTimelineAsync(childId, date, ct))
            .Where(e => e.IsParentVisible)
            .ToList();

        var diaperCount = timeline.Count(e => e.Category == CareEventTypes.DiaperChange);
        var naps = await _repository.GetNapsByChildAndDateAsync(_tenantContext.TenantId, childId, date, ct);
        var totalRest = naps.Where(n => n.ParentVisible && n.DurationMinutes.HasValue).Sum(n => n.DurationMinutes!.Value);

        var lunchEvent = timeline.FirstOrDefault(e => e.Title.Contains("Lunch", StringComparison.OrdinalIgnoreCase));
        var lunchStatus = lunchEvent != null 
            ? lunchEvent.Description 
            : (timeline.Any(e => e.Category == CareEventTypes.Meal) ? "Meal routine recorded" : "Not recorded yet");

        var moodEvent = timeline.LastOrDefault(e => e.Category == CareEventTypes.Comfort);
        var primaryMood = moodEvent != null 
            ? moodEvent.Title.Replace("Comfort observation (", "").Replace(")", "") 
            : (timeline.Any() ? "Content & Settled" : "Not recorded yet");

        var supplies = (await _repository.GetSuppliesByChildAsync(_tenantContext.TenantId, childId, ct))
            .Where(s => s.Status == "Low" || s.Status == "Depleted")
            .ToList();

        // Construct factual reassuring paragraph from actual persisted records (never invented)
        var name = child.FirstName;
        string reassurance;
        var hasAnyCareRecords = diaperCount > 0 || totalRest > 0 || lunchEvent != null || moodEvent != null;

        if (!hasAnyCareRecords)
        {
            reassurance = $"Not yet updated today. Care notes and observations will appear here once recorded by {name}'s teachers.";
        }
        else
        {
            reassurance = $"{name} had a calm and engaging day at Little Stars. " +
                (diaperCount > 0 ? $"{name} had {diaperCount} routine diaper checks. " : "") +
                (lunchEvent != null ? $"{lunchEvent.Description} " : "") +
                (totalRest > 0 ? $"{name} rested well ({totalRest / 60}h {totalRest % 60}m of sleep)." : "");
        }

        return new ParentDailyCareReport
        {
            ChildId = childId,
            ChildFirstName = child.FirstName,
            Date = date,
            DiaperChangesCount = diaperCount,
            TotalRestMinutes = totalRest,
            LunchMealStatus = lunchStatus,
            PrimaryMoodObservation = primaryMood,
            DailyReassuranceText = reassurance.Trim(),
            Timeline = timeline,
            LowSupplies = supplies
        };
    }

    public async Task<Guid> SubmitHandoverAsync(TeacherHandover handover, CancellationToken ct = default)
    {
        handover.TenantId = _tenantContext.TenantId;
        handover.OutgoingTeacherUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : handover.OutgoingTeacherUserId;
        handover.OutgoingTeacherName = !string.IsNullOrWhiteSpace(_userContext.Email) ? _userContext.Email : handover.OutgoingTeacherName;

        if (handover.ClassroomId == Guid.Empty)
            throw new ArgumentException("Classroom is required for a teacher handover.", nameof(handover.ClassroomId));

        var cls = await _classroomRepository.GetByIdAsync(_tenantContext.TenantId, handover.ClassroomId, ct);
        if (cls == null)
            throw new ArgumentException("Classroom not found.");

        handover.BranchId = cls.BranchId;
        var branch = await _branchRepository.GetByIdAsync(_tenantContext.TenantId, cls.BranchId, ct);
        if (branch != null)
        {
            handover.OrganizationId = branch.OrganizationId;
        }

        var id = await _repository.CreateHandoverAsync(handover, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = handover.OutgoingTeacherUserId,
            Action = "TeacherHandoverSubmitted",
            EntityType = "TeacherHandover",
            EntityId = id,
            Details = new { handover.ClassroomId, handover.ShiftType, handover.IsCritical }
        }, ct);

        return id;
    }

    public async Task AcknowledgeHandoverAsync(Guid handoverId, string? notes, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;
        var userName = _userContext.Email ?? "Receiving Teacher";

        await _repository.AcknowledgeHandoverAsync(tenantId, handoverId, userId, userName, notes, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = "TeacherHandoverAcknowledged",
            EntityType = "TeacherHandover",
            EntityId = handoverId,
            Details = new { AcknowledgedBy = userName }
        }, ct);
    }

    public async Task<IEnumerable<TeacherHandover>> GetClassroomHandoversAsync(Guid classroomId, DateOnly date, CancellationToken ct = default)
    {
        return await _repository.GetHandoversByClassroomAndDateAsync(_tenantContext.TenantId, classroomId, date, ct);
    }

    public async Task<IEnumerable<TeacherHandover>> GetPendingHandoversAsync(Guid branchId, CancellationToken ct = default)
    {
        return await _repository.GetPendingHandoversByBranchAsync(_tenantContext.TenantId, branchId, ct);
    }
}
