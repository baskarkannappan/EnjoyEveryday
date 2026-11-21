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


public partial class ChildDailyCareService
{
    public async Task<IEnumerable<ChildTimelineEntry>> GetChildCareTimelineAsync(Guid childId, DateOnly date, CancellationToken ct = default)
    {
        return await GetChildCareTimelineAsync(_tenantContext.TenantId, childId, date, ct);
    }

    public async Task<IEnumerable<ChildTimelineEntry>> GetChildCareTimelineAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;
        var entries = new List<ChildTimelineEntry>();

        // 1. Attendance Check-In
        var child = await _childRepository.GetByIdAsync(effectiveTenantId, childId, ct);
        if (child != null)
        {
            var (branchId, _) = await ResolveChildScopeAsync(child, ct);
            var attList = await _attendanceRepository.GetChildAttendanceByDateAsync(
                effectiveTenantId,
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
        var careEvents = await _repository.GetEventsByChildAndDateAsync(effectiveTenantId, childId, date, ct);
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
            Action = AuditActions.DailyCare.PlanUpdated,
            EntityType = AuditEntityTypes.ChildCarePlan,
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
            Action = AuditActions.DailyCare.SupplyUpdated,
            EntityType = AuditEntityTypes.ChildCareSupply,
            EntityId = supplyId,
            Details = $"Supply {supply.SupplyType} status updated to {supply.Status}."
        }, ct);

        return supplyId;
    }

    public async Task<ParentDailyCareReport> GetParentDailyCareReportAsync(Guid childId, DateOnly date, CancellationToken ct = default)
    {
        return await GetParentDailyCareReportAsync(_tenantContext.TenantId, childId, date, ct);
    }

    public async Task<ParentDailyCareReport> GetParentDailyCareReportAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId != Guid.Empty ? tenantId : _tenantContext.TenantId;
        var child = await _childRepository.GetByIdAsync(effectiveTenantId, childId, ct);
        if (child == null)
            throw new KeyNotFoundException("Child record not found.");

        var timeline = (await GetChildCareTimelineAsync(effectiveTenantId, childId, date, ct))
            .Where(e => e.IsParentVisible)
            .ToList();

        var diaperCount = timeline.Count(e => e.Category == CareEventTypes.DiaperChange);
        var naps = await _repository.GetNapsByChildAndDateAsync(effectiveTenantId, childId, date, ct);
        var totalRest = naps.Where(n => n.ParentVisible && n.DurationMinutes.HasValue).Sum(n => n.DurationMinutes!.Value);

        var lunchEvent = timeline.FirstOrDefault(e => e.Title.Contains("Lunch", StringComparison.OrdinalIgnoreCase));
        var lunchStatus = lunchEvent != null 
            ? lunchEvent.Description 
            : (timeline.Any(e => e.Category == CareEventTypes.Meal) ? "Meal routine recorded" : "Not recorded yet");

        var moodEvent = timeline.LastOrDefault(e => e.Category == CareEventTypes.Comfort);
        var primaryMood = moodEvent != null 
            ? moodEvent.Title.Replace("Comfort observation (", "").Replace(")", "") 
            : (timeline.Any() ? "Content & Settled" : "Not recorded yet");

        var supplies = (await _repository.GetSuppliesByChildAsync(effectiveTenantId, childId, ct))
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
            reassurance = $"{name} had a calm and engaging day today. " +
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

        if (_dailyOpsService != null && cls.BranchId != Guid.Empty)
        {
            await _dailyOpsService.ValidateBranchOperationsAllowedAsync(cls.BranchId, DateOnly.FromDateTime(DateTime.Today), DailyOperationReasons.TeacherHandover, ct);
            await _dailyOpsService.ValidateClassroomOperationsAllowedAsync(cls.Id, DateOnly.FromDateTime(DateTime.Today), DailyOperationReasons.TeacherHandover, ct);
        }

        var id = await _repository.CreateHandoverAsync(handover, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = handover.OutgoingTeacherUserId,
            Action = AuditActions.DailyCare.HandoverSubmitted,
            EntityType = AuditEntityTypes.TeacherHandover,
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
            Action = AuditActions.DailyCare.HandoverAcknowledged,
            EntityType = AuditEntityTypes.TeacherHandover,
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

    public async Task<ClassroomDailyCareFeedDto> GetClassroomDailyCareFeedAsync(Guid branchId, Guid? classroomId, DateOnly date, CancellationToken ct = default)
    {
        var overviewTask = GetClassroomOverviewAsync(branchId, classroomId, date, ct);
        var lowSuppliesTask = GetLowSuppliesAsync(branchId, ct);
        var pendingHandoversTask = GetPendingHandoversAsync(branchId, ct);
        var classroomHandoversTask = classroomId.HasValue
            ? GetClassroomHandoversAsync(classroomId.Value, date, ct)
            : null;

        await Task.WhenAll(
            overviewTask,
            lowSuppliesTask,
            pendingHandoversTask,
            classroomHandoversTask ?? Task.FromResult<IEnumerable<TeacherHandover>>(Array.Empty<TeacherHandover>()));

        var overview = (await overviewTask).ToList();
        var lowSupplies = (await lowSuppliesTask).ToList();
        var pendingHandovers = (await pendingHandoversTask).ToList();
        var classroomHandovers = classroomHandoversTask != null
            ? (await classroomHandoversTask).ToList()
            : pendingHandovers;

        var overviewDtos = overview.Select(c => new ClassroomChildCareStatusDto
        {
            ChildId = c.ChildId,
            ChildName = c.ChildName,
            PhotoUrl = c.PhotoUrl,
            ClassroomId = c.ClassroomId,
            ClassroomName = c.ClassroomName,
            IsCheckedIn = c.IsCheckedIn,
            AttendanceStatus = c.AttendanceStatus,
            ArrivalTime = c.ArrivalTime,
            DepartureTime = c.DepartureTime,
            LastDiaperEvent = c.LastDiaperEvent,
            LastMealEvent = c.LastMealEvent,
            ActiveNapSession = c.ActiveNapSession,
            LastComfortMood = c.LastComfortMood,
            TodayEventsCount = c.TodayEventsCount,
            HasLowSupplies = c.HasLowSupplies,
            ImportantCareAlert = c.ImportantCareAlert
        }).ToList();

        return new ClassroomDailyCareFeedDto
        {
            BranchId = branchId,
            ClassroomId = classroomId,
            Date = date,
            TotalRecordedEventsToday = overview.Sum(c => c.TodayEventsCount),
            ClassroomOverview = overviewDtos,
            LowSupplies = lowSupplies,
            PendingHandovers = pendingHandovers,
            ClassroomHandovers = classroomHandovers
        };
    }

    public async Task<ChildCarePackageDto> GetChildCarePackageAsync(Guid childId, DateOnly date, CancellationToken ct = default)
    {
        var timeline = (await GetChildCareTimelineAsync(childId, date, ct)).ToList();
        var supplies = (await GetChildSuppliesAsync(childId, ct)).ToList();
        var plan = await GetChildCarePlanAsync(childId, ct);

        var timelineDtos = timeline.Select(t => new ChildTimelineEntryDto
        {
            Id = t.Id,
            EventTime = t.EventTime,
            TimeLabel = t.TimeLabel,
            Title = t.Title,
            Description = t.Description,
            Category = t.Category,
            Icon = t.Icon,
            StaffName = t.StaffName,
            IsParentVisible = t.IsParentVisible,
            IsCorrected = t.IsCorrected
        }).ToList();

        return new ChildCarePackageDto
        {
            ChildId = childId,
            Date = date,
            ChildTimeline = timelineDtos,
            ChildSupplies = supplies,
            CurrentPlan = plan
        };
    }
}
