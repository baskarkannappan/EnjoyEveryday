using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class WeeklyPlanningAssistantTests
{
    private MockWeeklyPlanningRepository _planningRepo = null!;
    private MockClassroomRepository _classroomRepo = null!;
    private LocalMockExperienceRepository _experienceRepo = null!;
    private LocalMockExperienceFeedbackRepository _feedbackRepo = null!;
    private LocalMockExperienceScheduleRepository _scheduleRepo = null!;
    private ExperienceService _experienceService = null!;
    private PlannerService _plannerService = null!;
    private TenantSettingsService _settingsService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private WeeklyPlanningAssistantService _service = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _exp1Id = Guid.NewGuid();
    private readonly Guid _exp2Id = Guid.NewGuid();
    private readonly Guid _exp3Id = Guid.NewGuid();
    private readonly Guid _exp4Id = Guid.NewGuid();
    private readonly Guid _exp5Id = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _planningRepo = new MockWeeklyPlanningRepository();
        _classroomRepo = new MockClassroomRepository();
        _experienceRepo = new LocalMockExperienceRepository();
        _feedbackRepo = new LocalMockExperienceFeedbackRepository();
        _scheduleRepo = new LocalMockExperienceScheduleRepository();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "teacher.emily@enjoyeveryday.test",
            OrganizationId = Guid.NewGuid()
        };

        _experienceService = new ExperienceService(_experienceRepo, _feedbackRepo, _tenantContext, _userContext);
        _plannerService = new PlannerService(_scheduleRepo, _userContext);
        _settingsService = new TenantSettingsService(new LocalMockTenantSettingsRepository(), new LocalMockAesEncryptionService());

        // Seed Classroom
        _classroomRepo.Classrooms.Add(new Classroom
        {
            Id = _classroomId,
            TenantId = _tenantId,
            Name = "Bluebirds",
            AgeGroup = "Toddler",
            Capacity = 16,
            IsActive = true
        });

        // Seed Active Experiences in Library
        var expList = new[]
        {
            new Experience { Id = _exp1Id, TenantId = _tenantId, Title = "Building with Wooden Blocks", Description = "Creative construction and balance.", Status = "Active" },
            new Experience { Id = _exp2Id, TenantId = _tenantId, Title = "Storytelling Circle", Description = "Listening and sharing ideas.", Status = "Active" },
            new Experience { Id = _exp3Id, TenantId = _tenantId, Title = "Nature & Texture Exploration", Description = "Observing leaves and seeds.", Status = "Active" },
            new Experience { Id = _exp4Id, TenantId = _tenantId, Title = "Music & Rhythm Movement", Description = "Coordination and song rhythm.", Status = "Active" },
            new Experience { Id = _exp5Id, TenantId = _tenantId, Title = "Collaborative Finger Painting", Description = "Shared group art project.", Status = "Active" }
        };
        _experienceRepo.Experiences.AddRange(expList);

        var httpClient = new HttpClient(new FailingHttpMessageHandler());

        _service = new WeeklyPlanningAssistantService(
            _planningRepo,
            _classroomRepo,
            _experienceService,
            _plannerService,
            _scheduleRepo,
            _settingsService,
            _tenantContext,
            _userContext,
            httpClient,
            NullLogger<WeeklyPlanningAssistantService>.Instance
        );
    }

    [Test]
    public async Task GenerateWeeklyPlanDraftAsync_ValidClassroom_GeneratesFiveDayDraft()
    {
        var monday = new DateOnly(2026, 10, 12);
        var input = new GenerateWeeklyPlanInput
        {
            ClassroomId = _classroomId,
            WeekStartDate = monday,
            FocusAreas = "Nature, Storytelling",
            IndoorOutdoorPreference = "Balanced"
        };

        var draft = await _service.GenerateWeeklyPlanDraftAsync(input);

        Assert.That(draft, Is.Not.Null);
        Assert.That(draft.ClassroomId, Is.EqualTo(_classroomId));
        Assert.That(draft.Status, Is.EqualTo("Draft"));
        Assert.That(draft.Items, Is.Not.Null);
        Assert.That(draft.Items.Count, Is.EqualTo(5));

        // Monday to Friday alignment
        var days = draft.Items.Select(i => i.DayOfWeek).ToList();
        Assert.That(days, Is.EqualTo(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }));

        // Ensure all experiences are from the active library
        var validExpIds = new[] { _exp1Id, _exp2Id, _exp3Id, _exp4Id, _exp5Id };
        foreach (var item in draft.Items)
        {
            Assert.That(validExpIds, Does.Contain(item.ExperienceId));
            Assert.That(item.ExperienceTitle, Is.Not.Empty);
            Assert.That(item.Rationale, Is.Not.Empty);
        }

        // Verify persisted
        Assert.That(_planningRepo.Requests.Count, Is.EqualTo(1));
        Assert.That(_planningRepo.Items.Count, Is.EqualTo(5));
    }

    [Test]
    public void GenerateWeeklyPlanDraftAsync_UnauthorizedClassroom_ThrowsArgumentException()
    {
        var input = new GenerateWeeklyPlanInput
        {
            ClassroomId = Guid.NewGuid(), // alien classroom
            WeekStartDate = new DateOnly(2026, 10, 12)
        };

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _service.GenerateWeeklyPlanDraftAsync(input);
        });
    }

    [Test]
    public async Task GenerateWeeklyPlanDraftAsync_AvoidsRecentRepetition_FlagsWarningIfScheduledRecently()
    {
        var monday = new DateOnly(2026, 10, 12);

        // Pre-schedule exp1 in the previous week
        _scheduleRepo.Schedules.Add(new ExperienceSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            ExperienceId = _exp1Id,
            ScheduledDate = monday.AddDays(-3),
            Status = "Completed"
        });

        var input = new GenerateWeeklyPlanInput
        {
            ClassroomId = _classroomId,
            WeekStartDate = monday
        };

        var draft = await _service.GenerateWeeklyPlanDraftAsync(input);

        // If exp1 is selected, it should flag a warning note
        var exp1Item = draft.Items.FirstOrDefault(i => i.ExperienceId == _exp1Id);
        if (exp1Item != null)
        {
            Assert.That(exp1Item.ValidationStatus, Is.EqualTo("Warning"));
            Assert.That(exp1Item.ValidationNotes, Does.Contain("recently"));
        }
    }

    [Test]
    public async Task UpdateDraftItemAsync_AllowsTeacherToEditOrReplaceActivity()
    {
        var monday = new DateOnly(2026, 10, 12);
        var input = new GenerateWeeklyPlanInput
        {
            ClassroomId = _classroomId,
            WeekStartDate = monday
        };
        var draft = await _service.GenerateWeeklyPlanDraftAsync(input);
        var mondayItem = draft.Items.First(i => i.DayOfWeek == "Monday");

        // Teacher replaces with exp5 and changes time
        mondayItem.ExperienceId = _exp5Id;
        mondayItem.TimeOfDay = "Afternoon";
        mondayItem.LearningGoal = "Custom teacher goal: collaborative art mural.";

        var updated = await _service.UpdateDraftItemAsync(mondayItem);

        Assert.That(updated.ExperienceId, Is.EqualTo(_exp5Id));
        Assert.That(updated.ExperienceTitle, Is.EqualTo("Collaborative Finger Painting"));
        Assert.That(updated.TimeOfDay, Is.EqualTo("Afternoon"));
        Assert.That(updated.LearningGoal, Does.Contain("Custom teacher goal"));
    }

    [Test]
    public async Task SavePlanToExperiencePlanningAsync_SchedulesApprovedPlanIntoCalendar()
    {
        var monday = new DateOnly(2026, 10, 12);
        var input = new GenerateWeeklyPlanInput
        {
            ClassroomId = _classroomId,
            WeekStartDate = monday
        };
        var draft = await _service.GenerateWeeklyPlanDraftAsync(input);

        var submitInput = new SubmitWeeklyPlanInput
        {
            DraftRequestId = draft.Id,
            ConfirmedItems = draft.Items,
            AutoConfirmSchedules = true
        };

        var approvedDraft = await _service.SavePlanToExperiencePlanningAsync(submitInput);

        Assert.That(approvedDraft.Status, Is.EqualTo("Approved"));

        // Verify schedules were actually created via PlannerService
        Assert.That(_scheduleRepo.Schedules.Count, Is.EqualTo(5));
        var created = _scheduleRepo.Schedules.Where(s => s.ClassroomId == _classroomId).ToList();
        Assert.That(created.Count, Is.EqualTo(5));
        Assert.That(created.All(s => s.Status == "Confirmed"), Is.True);

        // Idempotent test: subsequent save should not create duplicate schedules
        var repeatDraft = await _service.SavePlanToExperiencePlanningAsync(submitInput);
        Assert.That(_scheduleRepo.Schedules.Count, Is.EqualTo(5));
    }
}

// Mock Repository for Weekly Planning
public class MockWeeklyPlanningRepository : IWeeklyPlanningRepository
{
    public List<WeeklyPlanningDraftRequest> Requests { get; } = new();
    public List<WeeklyPlanningDraftItem> Items { get; } = new();

    public Task<WeeklyPlanningDraftRequest> CreateDraftRequestAsync(WeeklyPlanningDraftRequest request, IEnumerable<WeeklyPlanningDraftItem> items, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        Items.AddRange(items);
        request.Items = items.ToList();
        return Task.FromResult(request);
    }

    public Task<WeeklyPlanningDraftRequest?> GetDraftByIdAsync(Guid tenantId, Guid draftId, CancellationToken cancellationToken = default)
    {
        var req = Requests.FirstOrDefault(r => r.TenantId == tenantId && r.Id == draftId);
        if (req != null)
        {
            req.Items = Items.Where(i => i.RequestId == req.Id).ToList();
        }
        return Task.FromResult(req);
    }

    public Task<WeeklyPlanningDraftRequest?> GetLatestDraftForWeekAsync(Guid tenantId, Guid classroomId, DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        var req = Requests.Where(r => r.TenantId == tenantId && r.ClassroomId == classroomId && r.WeekStartDate == weekStartDate)
                          .OrderByDescending(r => r.CreatedAt)
                          .FirstOrDefault();
        if (req != null)
        {
            req.Items = Items.Where(i => i.RequestId == req.Id).ToList();
        }
        return Task.FromResult(req);
    }

    public Task<IEnumerable<WeeklyPlanningDraftItem>> GetItemsByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var items = Items.Where(i => i.TenantId == tenantId && i.RequestId == requestId);
        return Task.FromResult<IEnumerable<WeeklyPlanningDraftItem>>(items.ToList());
    }

    public Task UpdateDraftItemAsync(WeeklyPlanningDraftItem item, CancellationToken cancellationToken = default)
    {
        var idx = Items.FindIndex(i => i.Id == item.Id);
        if (idx >= 0) Items[idx] = item;
        return Task.CompletedTask;
    }

    public Task UpdateDraftStatusAsync(Guid tenantId, Guid draftId, string status, CancellationToken cancellationToken = default)
    {
        var req = Requests.FirstOrDefault(r => r.TenantId == tenantId && r.Id == draftId);
        if (req != null)
        {
            req.Status = status;
        }
        return Task.CompletedTask;
    }
}
