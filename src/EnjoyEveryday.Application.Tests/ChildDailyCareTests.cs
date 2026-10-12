using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class ChildDailyCareTests
{
    private MockChildDailyCareRepository _careRepo = null!;
    private MockChildRepository _childRepo = null!;
    private MockClassroomRepository _classroomRepo = null!;
    private MockBranchRepository _branchRepo = null!;
    private MockAttendanceRepository _attendanceRepo = null!;
    private MockAuditService _auditService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;

    private ChildDailyCareService _careService = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _careRepo = new MockChildDailyCareRepository();
        _childRepo = new MockChildRepository();
        _classroomRepo = new MockClassroomRepository();
        _branchRepo = new MockBranchRepository();
        _attendanceRepo = new MockAttendanceRepository();
        _auditService = new MockAuditService();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "teacher.sarah@enjoyeveryday.test",
            OrganizationId = _orgId
        };

        // Seed domain hierarchy
        _branchRepo.Branches.Add(new Branch
        {
            Id = _branchId,
            TenantId = _tenantId,
            OrganizationId = _orgId,
            Name = "Downtown Branch"
        });

        _classroomRepo.Classrooms.Add(new Classroom
        {
            Id = _classroomId,
            TenantId = _tenantId,
            BranchId = _branchId,
            Name = "Toddler Room 1"
        });

        _childRepo.Children.Add(new Child
        {
            Id = _childId,
            TenantId = _tenantId,
            FirstName = "Aarav",
            LastName = "Patel",
            ClassroomId = _classroomId
        });

        _careService = new ChildDailyCareService(
            _careRepo,
            _childRepo,
            _classroomRepo,
            _branchRepo,
            _attendanceRepo,
            _tenantContext,
            _userContext,
            _auditService);
    }

    [Test]
    public async Task RecordDiaperChange_PersistsEvent_AndLogsAudit()
    {
        var ev = new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.DiaperChange,
            DiaperCondition = "Wet",
            Notes = "Routine diaper change; child was cooperative.",
            ParentVisible = true
        };

        var eventId = await _careService.RecordCareEventAsync(ev);

        Assert.That(eventId, Is.Not.EqualTo(Guid.Empty));
        var saved = await _careRepo.GetCareEventByIdAsync(_tenantId, eventId);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.DiaperCondition, Is.EqualTo("Wet"));
        Assert.That(saved.RecordedByName, Is.EqualTo("teacher.sarah@enjoyeveryday.test"));

        Assert.That(_auditService.Entries.Any(l => l.Action == "CHILD_CARE_RECORDED"), Is.True);
    }

    [Test]
    public async Task RecordMeal_StoresDetails_Correctly()
    {
        var ev = new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.Meal,
            MealType = "Lunch",
            FoodOffered = "Lentil soup and pear slices",
            AmountConsumed = "Most",
            FluidOz = 4.5m,
            Notes = "Ate enthusiastically.",
            ParentVisible = true
        };

        var eventId = await _careService.RecordCareEventAsync(ev);
        var saved = await _careRepo.GetCareEventByIdAsync(_tenantId, eventId);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.MealType, Is.EqualTo("Lunch"));
        Assert.That(saved.AmountConsumed, Is.EqualTo("Most"));
        Assert.That(saved.FluidOz, Is.EqualTo(4.5m));
    }

    [Test]
    public async Task StartNap_ThenWakeChild_CalculatesDurationAutomatically()
    {
        // 1. Start nap
        var nap = await _careService.StartNapAsync(_childId, _classroomId, "Settled with soft blanket.");
        Assert.That(nap.Status, Is.EqualTo("Active"));
        Assert.That(nap.StartTime, Is.LessThanOrEqualTo(DateTime.UtcNow));

        // 2. Prevent duplicate overlapping nap
        var duplicateAttempt = await _careService.StartNapAsync(_childId, _classroomId, "Duplicate try");
        Assert.That(duplicateAttempt.Id, Is.EqualTo(nap.Id), "Should return existing active nap instead of duplicating.");

        // Fast forward 45 minutes
        nap.StartTime = DateTime.UtcNow.AddMinutes(-45);

        // 3. Wake child
        var completed = await _careService.WakeChildAsync(_childId, "WokeNaturally", "Woke refreshed.");
        Assert.That(completed, Is.Not.Null);
        Assert.That(completed!.Status, Is.EqualTo("Completed"));
        Assert.That(completed.DurationMinutes, Is.GreaterThanOrEqualTo(45));

        // Verify active nap cleared
        var activeAfterWake = await _careService.GetActiveNapAsync(_childId);
        Assert.That(activeAfterWake, Is.Null);
    }

    [Test]
    public async Task RecordToiletingAndHygiene_PersistsCorrectCategories()
    {
        var toiletEvent = new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.Toileting,
            ToiletingActivity = "Success",
            Notes = "Used toilet with verbal encouragement."
        };
        var tId = await _careService.RecordCareEventAsync(toiletEvent);

        var hygieneEvent = new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.Hygiene,
            HygieneType = "Handwashing",
            Notes = "Washed hands before lunch."
        };
        var hId = await _careService.RecordCareEventAsync(hygieneEvent);

        var timeline = (await _careService.GetChildCareTimelineAsync(_childId, DateOnly.FromDateTime(DateTime.Today))).ToList();
        Assert.That(timeline.Any(t => t.Category == CareEventTypes.Toileting), Is.True);
        Assert.That(timeline.Any(t => t.Category == CareEventTypes.Hygiene), Is.True);
    }

    [Test]
    public async Task RecordCorrection_RequiresReason_AndUpdatesEvent()
    {
        var ev = new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.DiaperChange,
            DiaperCondition = "Wet",
            Notes = "Initial entry"
        };
        var eventId = await _careService.RecordCareEventAsync(ev);

        // Attempting correction without reason throws ArgumentException
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _careService.RecordCorrectionAsync(eventId, "", e => e.DiaperCondition = "WetAndSoiled");
        });

        // Valid correction
        await _careService.RecordCorrectionAsync(eventId, "Corrected condition after checking basket", e =>
        {
            e.DiaperCondition = "WetAndSoiled";
        });

        var updated = await _careRepo.GetCareEventByIdAsync(_tenantId, eventId);
        Assert.That(updated!.DiaperCondition, Is.EqualTo("WetAndSoiled"));
        Assert.That(updated.IsCorrected, Is.True);
        Assert.That(updated.CorrectionReason, Is.EqualTo("Corrected condition after checking basket"));
    }

    [Test]
    public async Task CarePlanAndSupplies_ManageRoutinesAndLowStockWarnings()
    {
        // 1. Save Care Plan
        var plan = new ChildCarePlan
        {
            ChildId = _childId,
            DietaryAllergiesSummary = "No peanuts. Lactose-sensitive.",
            DiaperRoutineGuidance = "Check every 2 hours.",
            SleepRoutineGuidance = "Quiet nap 12:30 PM to 2:00 PM."
        };
        var planId = await _careService.SaveCarePlanAsync(plan);
        var retrievedPlan = await _careService.GetChildCarePlanAsync(_childId);
        Assert.That(retrievedPlan, Is.Not.Null);
        Assert.That(retrievedPlan!.DietaryAllergiesSummary, Does.Contain("No peanuts"));

        // 2. Supply tracking
        var supply = new ChildCareSupply
        {
            ChildId = _childId,
            SupplyType = "Diapers",
            Status = "Low",
            ThresholdNote = "3 diapers left in cubby"
        };
        await _careService.UpdateSupplyStatusAsync(supply);

        var supplies = (await _careService.GetChildSuppliesAsync(_childId)).ToList();
        Assert.That(supplies.Any(s => s.SupplyType == "Diapers" && s.Status == "Low"), Is.True);

        var branchLow = (await _careService.GetLowSuppliesAsync(_branchId)).ToList();
        Assert.That(branchLow.Any(s => s.ChildId == _childId), Is.True);
    }

    [Test]
    public async Task BulkCareOperations_RecordsGroupLunchAndGroupNap()
    {
        // Add a second child to the classroom
        var secondChildId = Guid.NewGuid();
        _childRepo.Children.Add(new Child
        {
            Id = secondChildId,
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            FirstName = "Liam",
            LastName = "Smith"
        });

        var childIds = new[] { _childId, secondChildId };

        // 1. Group Lunch: record meal for both children
        var recordedCount = await _careService.RecordBatchCareEventsAsync(
            childIds,
            CareEventTypes.Meal,
            "Lunch (All / Most)",
            "Enjoyed pasta and vegetable soup together.",
            true);

        Assert.That(recordedCount, Is.EqualTo(2));
        var eventsChild1 = (await _careRepo.GetEventsByChildAndDateAsync(_tenantId, _childId, DateOnly.FromDateTime(DateTime.UtcNow))).ToList();
        var eventsChild2 = (await _careRepo.GetEventsByChildAndDateAsync(_tenantId, secondChildId, DateOnly.FromDateTime(DateTime.UtcNow))).ToList();

        Assert.That(eventsChild1.Any(e => e.EventType == CareEventTypes.Meal && e.AmountConsumed == "Lunch (All / Most)"), Is.True);
        Assert.That(eventsChild2.Any(e => e.EventType == CareEventTypes.Meal && e.AmountConsumed == "Lunch (All / Most)"), Is.True);

        // 2. Group Nap: start nap for both children
        var napStartedCount = await _careService.StartBatchNapAsync(childIds, _classroomId, "Classroom rest music on");
        Assert.That(napStartedCount, Is.EqualTo(2));

        var active1 = await _careService.GetActiveNapAsync(_childId);
        var active2 = await _careService.GetActiveNapAsync(secondChildId);
        Assert.That(active1, Is.Not.Null);
        Assert.That(active2, Is.Not.Null);

        // 3. Group Wake: wake both children
        var wokeCount = await _careService.WakeBatchChildrenAsync(childIds, "WokeNaturally", "Gentle wake up");
        Assert.That(wokeCount, Is.EqualTo(2));

        var afterWake1 = await _careService.GetActiveNapAsync(_childId);
        var afterWake2 = await _careService.GetActiveNapAsync(secondChildId);
        Assert.That(afterWake1, Is.Null);
        Assert.That(afterWake2, Is.Null);
    }

    [Test]
    public async Task ParentDailyCareReport_GeneratesFactualReport_MatchingScreenshot3()
    {
        // Log 2 diaper changes
        await _careService.RecordCareEventAsync(new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.DiaperChange,
            DiaperCondition = "Wet"
        });
        await _careService.RecordCareEventAsync(new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.DiaperChange,
            DiaperCondition = "WetAndSoiled"
        });

        // Log lunch
        await _careService.RecordCareEventAsync(new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.Meal,
            MealType = "Lunch",
            AmountConsumed = "Most",
            Notes = "Ate most of the meal."
        });

        // Log nap (80 min)
        var nap = new ChildNapSession
        {
            ChildId = _childId,
            StartTime = DateTime.Today.AddHours(12),
            EndTime = DateTime.Today.AddHours(13).AddMinutes(20),
            DurationMinutes = 80,
            Status = "Completed",
            ParentVisible = true
        };
        await _careRepo.StartNapSessionAsync(nap);

        // Log comfort mood
        await _careService.RecordCareEventAsync(new ChildCareEvent
        {
            ChildId = _childId,
            EventType = CareEventTypes.Comfort,
            ComfortMood = "Happy",
            Notes = "Happy playing with blocks."
        });

        var report = await _careService.GetParentDailyCareReportAsync(_childId, DateOnly.FromDateTime(DateTime.Today));

        Assert.That(report.DiaperChangesCount, Is.EqualTo(2));
        Assert.That(report.TotalRestMinutes, Is.EqualTo(80));
        Assert.That(report.RestTimeFormatted, Is.EqualTo("1h 20m"));
        Assert.That(report.DailyReassuranceText, Does.Contain("Aarav"));
        Assert.That(report.DailyReassuranceText, Does.Contain("1h 20m"));
    }
}

// ==========================================
// MOCK REPOSITORIES FOR NUNIT
// ==========================================

public class MockChildDailyCareRepository : IChildDailyCareRepository
{
    public List<ChildCareEvent> Events = new();
    public List<ChildNapSession> Naps = new();
    public List<ChildCarePlan> Plans = new();
    public List<ChildCareSupply> Supplies = new();

    public Task<Guid> AddCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default)
    {
        if (ev.Id == Guid.Empty) ev.Id = Guid.NewGuid();
        Events.Add(ev);
        return Task.FromResult(ev.Id);
    }

    public Task UpdateCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default)
    {
        var idx = Events.FindIndex(e => e.Id == ev.Id);
        if (idx >= 0) Events[idx] = ev;
        return Task.CompletedTask;
    }

    public Task<ChildCareEvent?> GetCareEventByIdAsync(Guid tenantId, Guid eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Events.FirstOrDefault(e => e.Id == eventId));
    }

    public Task<IEnumerable<ChildCareEvent>> GetEventsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildCareEvent>>(Events.Where(e => e.ChildId == childId && (DateOnly.FromDateTime(e.EventTime) == date || DateOnly.FromDateTime(e.EventTime.ToLocalTime()) == date)));
    }

    public Task<IEnumerable<ChildCareEvent>> GetEventsByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildCareEvent>>(Events.Where(e => e.ChildId == childId).OrderByDescending(e => e.EventTime));
    }

    public Task<IEnumerable<ChildCareEvent>> GetEventsByClassroomAndDateAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildCareEvent>>(Events.Where(e => e.ClassroomId == classroomId && (DateOnly.FromDateTime(e.EventTime) == date || DateOnly.FromDateTime(e.EventTime.ToLocalTime()) == date)));
    }

    public Task<IEnumerable<ChildCareEvent>> GetEventsByBranchAndDateAsync(Guid tenantId, Guid branchId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildCareEvent>>(Events.Where(e => e.BranchId == branchId && (DateOnly.FromDateTime(e.EventTime) == date || DateOnly.FromDateTime(e.EventTime.ToLocalTime()) == date)));
    }

    public Task<Guid> StartNapSessionAsync(ChildNapSession session, CancellationToken cancellationToken = default)
    {
        if (session.Id == Guid.Empty) session.Id = Guid.NewGuid();
        Naps.Add(session);
        return Task.FromResult(session.Id);
    }

    public Task EndNapSessionAsync(Guid tenantId, Guid sessionId, DateTime endTime, int durationMinutes, string? outcome, string? notes, Guid endedByUserId, string? endedByName, CancellationToken cancellationToken = default)
    {
        var nap = Naps.FirstOrDefault(n => n.Id == sessionId);
        if (nap != null)
        {
            nap.EndTime = endTime;
            nap.DurationMinutes = durationMinutes;
            nap.Outcome = outcome;
            nap.Status = "Completed";
            if (!string.IsNullOrEmpty(notes)) nap.Notes = notes;
        }
        return Task.CompletedTask;
    }

    public Task<ChildNapSession?> GetActiveNapSessionAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Naps.FirstOrDefault(n => n.ChildId == childId && n.Status == "Active"));
    }

    public Task<IEnumerable<ChildNapSession>> GetActiveNapsByClassroomAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildNapSession>>(Naps.Where(n => n.ClassroomId == classroomId && n.Status == "Active"));
    }

    public Task<IEnumerable<ChildNapSession>> GetNapsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildNapSession>>(Naps.Where(n => n.ChildId == childId && DateOnly.FromDateTime(n.StartTime.Date) == date));
    }

    public Task<ChildCarePlan?> GetActiveCarePlanAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Plans.FirstOrDefault(p => p.ChildId == childId && p.Status == "Active"));
    }

    public Task<Guid> SaveCarePlanAsync(ChildCarePlan plan, CancellationToken cancellationToken = default)
    {
        if (plan.Id == Guid.Empty) plan.Id = Guid.NewGuid();
        Plans.Add(plan);
        return Task.FromResult(plan.Id);
    }

    public Task<IEnumerable<ChildCarePlan>> GetCarePlansByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildCarePlan>>(Plans.Where(p => p.BranchId == branchId && p.Status == "Active"));
    }

    public Task<IEnumerable<ChildCareSupply>> GetSuppliesByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildCareSupply>>(Supplies.Where(s => s.ChildId == childId));
    }

    public Task<IEnumerable<ChildCareSupply>> GetLowSuppliesByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildCareSupply>>(Supplies.Where(s => s.BranchId == branchId && (s.Status == "Low" || s.Status == "Depleted")));
    }

    public Task<Guid> SaveSupplyStatusAsync(ChildCareSupply supply, CancellationToken cancellationToken = default)
    {
        if (supply.Id == Guid.Empty) supply.Id = Guid.NewGuid();
        var idx = Supplies.FindIndex(s => s.Id == supply.Id);
        if (idx >= 0) Supplies[idx] = supply;
        else Supplies.Add(supply);
        return Task.FromResult(supply.Id);
    }
}

public class MockChildRepository : IChildRepository
{
    public List<Child> Children = new();

    public Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Children.FirstOrDefault(c => c.Id == id));
    }

    public Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Child>>(Children);
    }

    public Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default)
    {
        if (child.Id == Guid.Empty) child.Id = Guid.NewGuid();
        Children.Add(child);
        return Task.FromResult(child);
    }

    public Task UpdateAsync(Child child, CancellationToken cancellationToken = default)
    {
        var idx = Children.FindIndex(c => c.Id == child.Id);
        if (idx >= 0) Children[idx] = child;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        Children.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Child>>(Children.Where(c => c.ClassroomId == classroomId));
    }
}

public class MockClassroomRepository : IClassroomRepository
{
    public List<Classroom> Classrooms = new();

    public Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Classrooms.FirstOrDefault(c => c.Id == id));
    }

    public Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Classroom>>(Classrooms.Where(c => c.BranchId == branchId));
    }

    public Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Classroom>>(Classrooms);
    }

    public Task<Classroom> AddAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        if (classroom.Id == Guid.Empty) classroom.Id = Guid.NewGuid();
        Classrooms.Add(classroom);
        return Task.FromResult(classroom);
    }

    public Task UpdateAsync(Classroom classroom, CancellationToken cancellationToken = default)
    {
        var idx = Classrooms.FindIndex(c => c.Id == classroom.Id);
        if (idx >= 0) Classrooms[idx] = classroom;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        Classrooms.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}

public class MockAttendanceRepository : IAttendanceRepository
{
    public List<ChildAttendance> ChildAttendances = new();
    public List<TeacherAttendance> TeacherAttendances = new();

    public Task<IEnumerable<ChildAttendance>> GetChildAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ChildAttendance>>(ChildAttendances.Where(a => a.BranchId == branchId && a.AttendanceDate == date.Date));
    }

    public Task<IEnumerable<TeacherAttendance>> GetTeacherAttendanceByDateAsync(Guid tenantId, Guid branchId, DateTime date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<TeacherAttendance>>(TeacherAttendances.Where(a => a.BranchId == branchId && a.AttendanceDate == date.Date));
    }

    public Task<ChildAttendance> RecordChildAttendanceAsync(ChildAttendance attendance, CancellationToken cancellationToken = default)
    {
        if (attendance.Id == Guid.Empty) attendance.Id = Guid.NewGuid();
        ChildAttendances.Add(attendance);
        return Task.FromResult(attendance);
    }

    public Task<TeacherAttendance> RecordTeacherAttendanceAsync(TeacherAttendance attendance, CancellationToken cancellationToken = default)
    {
        if (attendance.Id == Guid.Empty) attendance.Id = Guid.NewGuid();
        TeacherAttendances.Add(attendance);
        return Task.FromResult(attendance);
    }

    public Task<AttendanceSummary> GetOrganizationSummaryAsync(Guid tenantId, DateTime date, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AttendanceSummary
        {
            ExpectedChildren = ChildAttendances.Count,
            PresentChildren = ChildAttendances.Count(a => a.Status == "Present")
        });
    }
}
