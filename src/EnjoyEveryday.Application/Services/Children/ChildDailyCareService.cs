using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Constants;
using EnjoyEveryday.Domain.Constants;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;


public partial class ChildDailyCareService : IChildDailyCareService
{
    private readonly IChildDailyCareRepository _repository;
    private readonly IChildRepository _childRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly IDailyOperationsService? _dailyOpsService;
    private readonly IBranchOperationalStatusService? _branchOperationalStatusService;

    public ChildDailyCareService(
        IChildDailyCareRepository repository,
        IChildRepository childRepository,
        IClassroomRepository classroomRepository,
        IBranchRepository branchRepository,
        IAttendanceRepository attendanceRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        IDailyOperationsService? dailyOpsService = null,
        IBranchOperationalStatusService? branchOperationalStatusService = null)
    {
        _repository = repository;
        _childRepository = childRepository;
        _classroomRepository = classroomRepository;
        _branchRepository = branchRepository;
        _attendanceRepository = attendanceRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _dailyOpsService = dailyOpsService;
        _branchOperationalStatusService = branchOperationalStatusService;
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

        if (_branchOperationalStatusService != null && branchId != Guid.Empty)
        {
            await _branchOperationalStatusService.EnsureBranchOperationalAsync(_tenantContext.TenantId, branchId, "care log", DateTimeOffset.UtcNow, ct);
        }

        if (_dailyOpsService != null && branchId != Guid.Empty)
        {
            await _dailyOpsService.ValidateBranchOperationsAllowedAsync(branchId, DateOnly.FromDateTime(DateTime.Today), $"{ev.EventType} care log", ct);
            var room = ev.ClassroomId ?? child.ClassroomId;
            if (room.HasValue && room.Value != Guid.Empty)
            {
                await _dailyOpsService.ValidateClassroomOperationsAllowedAsync(room.Value, DateOnly.FromDateTime(DateTime.Today), $"{ev.EventType} care log", ct);
            }
        }

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
            Action = AuditActions.DailyCare.Recorded,
            EntityType = AuditEntityTypes.ChildCareEvent,
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

            if (_branchOperationalStatusService != null && branchId != Guid.Empty)
        {
            await _branchOperationalStatusService.EnsureBranchOperationalAsync(_tenantContext.TenantId, branchId, "care log", DateTimeOffset.UtcNow, ct);
        }

        if (_dailyOpsService != null && branchId != Guid.Empty)
            {
                await _dailyOpsService.ValidateBranchOperationsAllowedAsync(branchId, DateOnly.FromDateTime(DateTime.Today), $"{eventType} care log", ct);
                if (child.ClassroomId.HasValue && child.ClassroomId.Value != Guid.Empty)
                {
                    await _dailyOpsService.ValidateClassroomOperationsAllowedAsync(child.ClassroomId.Value, DateOnly.FromDateTime(DateTime.Today), $"{eventType} care log", ct);
                }
            }

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
                Action = AuditActions.DailyCare.BatchRecorded,
                EntityType = AuditEntityTypes.ChildCareEvent,
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
            Action = AuditActions.DailyCare.Corrected,
            EntityType = AuditEntityTypes.ChildCareEvent,
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

        if (_branchOperationalStatusService != null && branchId != Guid.Empty)
        {
            await _branchOperationalStatusService.EnsureBranchOperationalAsync(_tenantContext.TenantId, branchId, "care log", DateTimeOffset.UtcNow, ct);
        }

        if (_dailyOpsService != null && branchId != Guid.Empty)
        {
            await _dailyOpsService.ValidateBranchOperationsAllowedAsync(branchId, DateOnly.FromDateTime(DateTime.Today), DailyOperationReasons.NapRoutine, ct);
            var room = classroomId ?? child.ClassroomId;
            if (room.HasValue && room.Value != Guid.Empty)
            {
                await _dailyOpsService.ValidateClassroomOperationsAllowedAsync(room.Value, DateOnly.FromDateTime(DateTime.Today), DailyOperationReasons.NapRoutine, ct);
            }
        }

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
            Action = AuditActions.DailyCare.NapStarted,
            EntityType = AuditEntityTypes.ChildNapSession,
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

        if (_dailyOpsService != null && activeSession.BranchId != Guid.Empty)
        {
            await _dailyOpsService.ValidateBranchOperationsAllowedAsync(activeSession.BranchId, DateOnly.FromDateTime(DateTime.Today), DailyOperationReasons.WakeUpRoutine, ct);
            if (activeSession.ClassroomId.HasValue && activeSession.ClassroomId.Value != Guid.Empty)
            {
                await _dailyOpsService.ValidateClassroomOperationsAllowedAsync(activeSession.ClassroomId.Value, DateOnly.FromDateTime(DateTime.Today), DailyOperationReasons.WakeUpRoutine, ct);
            }
        }

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
            Action = AuditActions.DailyCare.NapEnded,
            EntityType = AuditEntityTypes.ChildNapSession,
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
        List<Child> children;
        if (classroomId.HasValue)
        {
            children = (await _childRepository.GetByClassroomIdAsync(_tenantContext.TenantId, classroomId.Value, ct)).ToList();
        }
        else
        {
            var branchClassrooms = (await _classroomRepository.GetByBranchIdAsync(_tenantContext.TenantId, branchId, ct)).ToList();
            var branchClassroomIds = branchClassrooms.Select(c => c.Id).ToHashSet();
            var allChildren = await _childRepository.GetAllAsync(_tenantContext.TenantId, ct);
            children = allChildren
                .Where(c => c.ClassroomId.HasValue && branchClassroomIds.Contains(c.ClassroomId.Value))
                .ToList();
        }

        var attendancesList = await _attendanceRepository.GetChildAttendanceByDateAsync(
            _tenantContext.TenantId,
            branchId,
            date.ToDateTime(TimeOnly.MinValue),
            ct);
        var attendances = attendancesList
            .GroupBy(a => a.ChildId)
            .ToDictionary(
                g => g.Key, 
                g => g.OrderByDescending(a => a.Status == "CheckedOut" ? 2 : (a.Status == "Present" ? 1 : 0))
                      .ThenByDescending(a => a.DepartureTime ?? a.ArrivalTime ?? TimeSpan.Zero)
                      .First());

        var events = classroomId.HasValue
            ? (await _repository.GetEventsByClassroomAndDateAsync(_tenantContext.TenantId, classroomId.Value, date, ct)).ToList()
            : (await _repository.GetEventsByBranchAndDateAsync(_tenantContext.TenantId, branchId, date, ct)).ToList();

        var eventsByChild = events.GroupBy(e => e.ChildId).ToDictionary(g => g.Key, g => g.ToList());

        var targetDateTime = date.ToDateTime(TimeOnly.MinValue);
        var activeNaps = classroomId.HasValue
            ? (await _repository.GetActiveNapsByClassroomAsync(_tenantContext.TenantId, classroomId.Value, ct))
                .Where(n => n.StartTime.Date == targetDateTime.Date)
                .GroupBy(n => n.ChildId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(n => n.StartTime).First())
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

            bool isDeparted = string.Equals(att?.Status, "CheckedOut", StringComparison.OrdinalIgnoreCase) || att?.DepartureTime != null;
            bool isPresent = string.Equals(att?.Status, "Present", StringComparison.OrdinalIgnoreCase) && !isDeparted;
            string attendanceStatus = isDeparted ? "CheckedOut" : (att?.Status ?? "Not Checked In");

            var status = new ClassroomChildCareStatus
            {
                ChildId = child.Id,
                ChildName = $"{child.FirstName} {child.LastName}",
                PhotoUrl = child.PhotoUrl,
                ClassroomId = child.ClassroomId,
                IsCheckedIn = isPresent,
                AttendanceStatus = attendanceStatus,
                ArrivalTime = att?.ArrivalTime,
                DepartureTime = att?.DepartureTime,
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

}
