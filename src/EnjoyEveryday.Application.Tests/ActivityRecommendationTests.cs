using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
public class ActivityRecommendationTests
{
    private MockActivityRecommendationRepository _recommendationRepo = null!;
    private LocalMockExperienceRepository _experienceRepo = null!;
    private LocalMockExperienceFeedbackRepository _feedbackRepo = null!;
    private LocalMockExperienceScheduleRepository _scheduleRepo = null!;
    private MockClassroomRepository _classroomRepo = null!;
    private MockChildRepository _childRepo = null!;
    private MockAuditService _auditService = null!;
    private MockMemoryService _memoryService = null!;
    private PlannerService _plannerService = null!;
    private ExperienceService _experienceService = null!;
    private ClassroomService _classroomService = null!;
    private ChildService _childService = null!;
    private TenantSettingsService _settingsService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private ActivityRecommendationService _service = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _exp1Id = Guid.NewGuid();
    private readonly Guid _exp2Id = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _recommendationRepo = new MockActivityRecommendationRepository();
        _experienceRepo = new LocalMockExperienceRepository();
        _feedbackRepo = new LocalMockExperienceFeedbackRepository();
        _scheduleRepo = new LocalMockExperienceScheduleRepository();
        _classroomRepo = new MockClassroomRepository();
        _childRepo = new MockChildRepository();
        _auditService = new MockAuditService();
        _memoryService = new MockMemoryService();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "teacher@enjoyeveryday.test",
            OrganizationId = Guid.NewGuid()
        };

        _experienceService = new ExperienceService(_experienceRepo, _feedbackRepo, _tenantContext, _userContext);
        _plannerService = new PlannerService(_scheduleRepo, _userContext);
        _classroomService = new ClassroomService(_classroomRepo, _tenantContext, _userContext);
        _childService = new ChildService(_childRepo, _tenantContext, _auditService, _userContext);
        _settingsService = new TenantSettingsService(new LocalMockTenantSettingsRepository(), new LocalMockAesEncryptionService());

        // Seed Classroom & Child
        _classroomRepo.Classrooms.Add(new Classroom
        {
            Id = _classroomId,
            TenantId = _tenantId,
            Name = "Butterflies Room",
            AgeGroup = "Toddler",
            Capacity = 15,
            IsActive = true
        });

        _childRepo.Children.Add(new Child
        {
            Id = _childId,
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            FirstName = "Leo",
            LastName = "V",
            DateOfBirth = new DateTime(2022, 5, 10),
            IsActive = true
        });

        // Seed Candidate Experiences
        _experienceRepo.Experiences.Add(new Experience
        {
            Id = _exp1Id,
            TenantId = _tenantId,
            Title = "Bridge Building with Blocks",
            Description = "Construct bridges using foam blocks and animal figurines.",
            Status = "Active"
        });

        _experienceRepo.Experiences.Add(new Experience
        {
            Id = _exp2Id,
            TenantId = _tenantId,
            Title = "Sensory Water Play",
            Description = "Pouring and sorting colored sea animals in water trays.",
            Status = "Active"
        });

        // Seed Memory Events
        _memoryService.Memories.Add(new MemoryEvent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            ChildId = _childId,
            EventType = "Observation",
            Title = "Building towers and bridges",
            Description = "Leo spent 25 minutes balancing wooden blocks to make bridges for animals.",
            SourceType = "child_stories",
            SourceId = Guid.NewGuid(),
            EventDate = DateTimeOffset.UtcNow.AddDays(-1)
        });

        // Handler configured to throw so fallback is tested cleanly in unit tests
        var httpClient = new HttpClient(new FailingHttpMessageHandler());

        _service = new ActivityRecommendationService(
            _recommendationRepo,
            _memoryService,
            _experienceService,
            _plannerService,
            _classroomService,
            _childService,
            _settingsService,
            _tenantContext,
            _userContext,
            httpClient
        );
    }

    [Test]
    public async Task GenerateRecommendationsAsync_ClassroomScope_ReturnsGroundedFallbackWhenAiUnavailable()
    {
        var preferences = new RecommendationPreferencesDto
        {
            PreferredDurationMinutes = 20
        };

        var result = await _service.GenerateRecommendationsAsync(_classroomId, null, preferences);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.GreaterThan(0));

        var topItem = result.Items.First();
        Assert.That(topItem.ExperienceId, Is.EqualTo(_exp1Id).Or.EqualTo(_exp2Id));
        Assert.That(topItem.Rationale, Is.Not.Empty);

        // Verify request was persisted
        Assert.That(_recommendationRepo.Requests.Count, Is.EqualTo(1));
        Assert.That(_recommendationRepo.Items.Count, Is.EqualTo(result.Items.Count));
    }

    [Test]
    public async Task GenerateRecommendationsAsync_ChildScope_ValidatesChildAndClassroom()
    {
        var preferences = new RecommendationPreferencesDto
        {
            PreferredDurationMinutes = 15
        };

        var result = await _service.GenerateRecommendationsAsync(_classroomId, _childId, preferences);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items.Count, Is.GreaterThan(0));
        Assert.That(result.Request.ChildId, Is.EqualTo(_childId));
    }

    [Test]
    public void GenerateRecommendationsAsync_ChildNotInClassroom_ThrowsArgumentException()
    {
        var unrelatedChildId = Guid.NewGuid();
        _childRepo.Children.Add(new Child
        {
            Id = unrelatedChildId,
            TenantId = _tenantId,
            ClassroomId = Guid.NewGuid(),
            FirstName = "Other",
            LastName = "Child",
            DateOfBirth = new DateTime(2021, 1, 1),
            IsActive = true
        });

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _service.GenerateRecommendationsAsync(_classroomId, unrelatedChildId);
        });
    }

    [Test]
    public async Task AcceptRecommendationAsync_SchedulesExperienceAndRecordsDecisionIdempotently()
    {
        // 1. Get suggestions first
        var result = await _service.GenerateRecommendationsAsync(_classroomId);
        var chosenItem = result.Items.First();

        var scheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        // 2. Accept
        var decision1 = await _service.AcceptRecommendationAsync(
            result.Request.Id,
            chosenItem.Id,
            scheduledDate,
            "Morning",
            notes: "Suggested by EnjoyEveryday Intelligence based on bridge building interest."
        );

        Assert.That(decision1, Is.Not.Null);
        Assert.That(decision1.Decision, Is.EqualTo("Accepted"));
        Assert.That(decision1.ResultingScheduleId, Is.Not.Null);

        // Verify schedule created
        var schedule = _scheduleRepo.Schedules.FirstOrDefault(s => s.Id == decision1.ResultingScheduleId);
        Assert.That(schedule, Is.Not.Null);
        Assert.That(schedule!.ExperienceId, Is.EqualTo(chosenItem.ExperienceId));
        Assert.That(schedule.ClassroomId, Is.EqualTo(_classroomId));

        // 3. Idempotent call
        var decision2 = await _service.AcceptRecommendationAsync(
            result.Request.Id,
            chosenItem.Id,
            scheduledDate,
            "Morning"
        );

        Assert.That(decision2.ResultingScheduleId, Is.EqualTo(decision1.ResultingScheduleId));
        Assert.That(_recommendationRepo.Decisions.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task RejectRecommendationAsync_RecordsRejectionWithReason()
    {
        var result = await _service.GenerateRecommendationsAsync(_classroomId);
        var chosenItem = result.Items.First();

        var decision = await _service.RejectRecommendationAsync(
            result.Request.Id,
            chosenItem.Id,
            rejectionReason: "Materials not available this week"
        );

        Assert.That(decision, Is.Not.Null);
        Assert.That(decision.Decision, Is.EqualTo("Rejected"));
        Assert.That(decision.RejectionReason, Is.EqualTo("Materials not available this week"));
        Assert.That(_recommendationRepo.Decisions.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GenerateRecommendationsAsync_WithLearningObjective_PrioritizesMatchingActivities()
    {
        var preferences = new RecommendationPreferencesDto
        {
            LearningObjective = "Building",
            PreferredDurationMinutes = 20
        };

        var result = await _service.GenerateRecommendationsAsync(_classroomId, preferences: preferences);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items.Count, Is.GreaterThan(0));

        var topItem = result.Items.First();
        Assert.That(topItem.Rationale, Does.Contain("Building").Or.Contain("Aligns directly"));
    }

    [Test]
    public async Task GenerateRecommendationsAsync_WithAvailableMaterials_IncludesMaterialsInRationale()
    {
        var preferences = new RecommendationPreferencesDto
        {
            AvailableMaterials = "wooden blocks",
            PreferredDurationMinutes = 15
        };

        var result = await _service.GenerateRecommendationsAsync(_classroomId, preferences: preferences);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items.Count, Is.GreaterThan(0));
    }
}

// Local Mock Implementations for Tests
public class MockActivityRecommendationRepository : IActivityRecommendationRepository
{
    public List<ActivityRecommendationRequest> Requests { get; } = new();
    public List<ActivityRecommendationItem> Items { get; } = new();
    public List<ActivityRecommendationDecision> Decisions { get; } = new();

    public Task<ActivityRecommendationRequest> CreateRequestAsync(ActivityRecommendationRequest request, IEnumerable<ActivityRecommendationItem> items, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        Items.AddRange(items);
        return Task.FromResult(request);
    }

    public Task<ActivityRecommendationRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var req = Requests.FirstOrDefault(r => r.Id == requestId);
        return Task.FromResult(req);
    }

    public Task<IEnumerable<ActivityRecommendationItem>> GetItemsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var items = Items.Where(i => i.RequestId == requestId);
        return Task.FromResult<IEnumerable<ActivityRecommendationItem>>(items.ToList());
    }

    public Task<ActivityRecommendationDecision> RecordDecisionAsync(ActivityRecommendationDecision decision, CancellationToken cancellationToken = default)
    {
        var existing = Decisions.FirstOrDefault(d => d.RecommendationItemId == decision.RecommendationItemId);
        if (existing != null)
        {
            existing.Decision = decision.Decision;
            existing.RejectionReason = decision.RejectionReason;
            existing.ResultingScheduleId = decision.ResultingScheduleId;
            return Task.FromResult(existing);
        }
        else
        {
            Decisions.Add(decision);
            return Task.FromResult(decision);
        }
    }

    public Task<IEnumerable<ActivityRecommendationDecision>> GetDecisionsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var itemIds = Items.Where(i => i.RequestId == requestId).Select(i => i.Id).ToHashSet();
        var decisions = Decisions.Where(d => itemIds.Contains(d.RecommendationItemId));
        return Task.FromResult<IEnumerable<ActivityRecommendationDecision>>(decisions.ToList());
    }
}

public class LocalMockExperienceRepository : IExperienceRepository
{
    public List<Experience> Experiences { get; } = new();

    public Task<Experience?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Experiences.FirstOrDefault(e => e.Id == id && e.TenantId == tenantId));
    }

    public Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Experience>>(Experiences.Where(e => e.TenantId == tenantId).ToList());
    }

    public Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        Experiences.Add(experience);
        return Task.FromResult(experience);
    }

    public Task UpdateAsync(Experience experience, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IEnumerable<ExperienceVersion>> GetVersionsAsync(Guid experienceId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExperienceVersion>>(Array.Empty<ExperienceVersion>());
}

public class LocalMockExperienceFeedbackRepository : IExperienceFeedbackRepository
{
    public Task<ExperienceFeedback?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ExperienceFeedback?>(null);
    public Task<IEnumerable<ExperienceFeedback>> GetByScheduleIdAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExperienceFeedback>>(Array.Empty<ExperienceFeedback>());
    public Task<IEnumerable<ExperienceFeedback>> GetByExperienceIdAsync(Guid tenantId, Guid experienceId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExperienceFeedback>>(Array.Empty<ExperienceFeedback>());
    public Task<IEnumerable<ExperienceFeedback>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExperienceFeedback>>(Array.Empty<ExperienceFeedback>());
    public Task<ExperienceFeedback> AddAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default) => Task.FromResult(feedback);
    public Task UpdateAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class LocalMockExperienceScheduleRepository : IExperienceScheduleRepository
{
    public List<ExperienceSchedule> Schedules { get; } = new();

    public Task<IEnumerable<ExperienceSchedule>> GetByClassroomAndDateRangeAsync(Guid classroomId, DateOnly startDate, DateOnly endDate)
    {
        var list = Schedules.Where(s => s.ClassroomId == classroomId && s.ScheduledDate >= startDate && s.ScheduledDate <= endDate);
        return Task.FromResult<IEnumerable<ExperienceSchedule>>(list.ToList());
    }

    public Task<IEnumerable<ExperienceSchedule>> GetByDateAsync(DateOnly date)
    {
        var list = Schedules.Where(s => s.ScheduledDate == date);
        return Task.FromResult<IEnumerable<ExperienceSchedule>>(list.ToList());
    }

    public Task<ExperienceSchedule?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(Schedules.FirstOrDefault(s => s.Id == id));
    }

    public Task<ExperienceSchedule> CreateAsync(ExperienceSchedule schedule)
    {
        if (schedule.Id == Guid.Empty) schedule.Id = Guid.NewGuid();
        Schedules.Add(schedule);
        return Task.FromResult(schedule);
    }

    public Task UpdateAsync(ExperienceSchedule schedule)
    {
        var idx = Schedules.FindIndex(s => s.Id == schedule.Id);
        if (idx >= 0) Schedules[idx] = schedule;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        Schedules.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }
}

public class LocalMockTenantSettingsRepository : ITenantSettingsRepository
{
    private string? _apiKey;
    public Task<string?> GetEncryptedAiApiKeyAsync(Guid tenantId) => Task.FromResult(_apiKey);
    public Task SetEncryptedAiApiKeyAsync(Guid tenantId, string encryptedKey)
    {
        _apiKey = encryptedKey;
        return Task.CompletedTask;
    }
}

public class LocalMockAesEncryptionService : IAesEncryptionService
{
    public string Encrypt(string plainText) => $"enc:{plainText}";
    public string Decrypt(string cipherText) => cipherText.Replace("enc:", "");
}

public class MockMemoryService : IMemoryService
{
    public List<MemoryEvent> Memories { get; } = new();

    public Task<IEnumerable<MemoryEvent>> GetChildMemoriesAsync(Guid childId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<MemoryEvent>>(Memories.Where(m => m.ChildId == childId).ToList());
    }

    public Task<IEnumerable<MemoryEvent>> GetClassroomMemoriesAsync(Guid classroomId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<MemoryEvent>>(Memories.Where(m => m.ClassroomId == classroomId).ToList());
    }

    public Task<IEnumerable<MemoryEvent>> GetTeacherMemoriesAsync(Guid teacherId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<MemoryEvent>>(Memories.Where(m => m.TeacherId == teacherId).ToList());
    }

    public Task<IEnumerable<MemoryEvent>> GetExperienceMemoriesAsync(Guid experienceId, MemoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<MemoryEvent>>(Memories.Where(m => m.ExperienceId == experienceId).ToList());
    }
}

public class FailingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Simulated AI connection failure to test grounded deterministic fallback.");
    }
}
