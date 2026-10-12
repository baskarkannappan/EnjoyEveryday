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
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class ChildJourneyInsightTests
{
    private MockChildJourneyInsightRepository _insightRepo = null!;
    private MockChildRepository _childRepo = null!;
    private MockClassroomRepository _classroomRepo = null!;
    private LocalMockExperienceRepository _experienceRepo = null!;
    private LocalMockExperienceFeedbackRepository _feedbackRepo = null!;
    private MockAuditService _auditService = null!;
    private MockMemoryService _memoryService = null!;
    private ChildService _childService = null!;
    private ExperienceService _experienceService = null!;
    private TenantSettingsService _settingsService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private ChildJourneyInsightService _service = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _memoryStoryId = Guid.NewGuid();
    private readonly Guid _memorySessionId = Guid.NewGuid();
    private readonly Guid _experienceId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _insightRepo = new MockChildJourneyInsightRepository();
        _childRepo = new MockChildRepository();
        _classroomRepo = new MockClassroomRepository();
        _experienceRepo = new LocalMockExperienceRepository();
        _feedbackRepo = new LocalMockExperienceFeedbackRepository();
        _auditService = new MockAuditService();
        _memoryService = new MockMemoryService();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "teacher.rachel@enjoyeveryday.test",
            OrganizationId = Guid.NewGuid()
        };

        _childService = new ChildService(_childRepo, _tenantContext, _auditService, _userContext);
        _experienceService = new ExperienceService(_experienceRepo, _feedbackRepo, _tenantContext, _userContext);
        _settingsService = new TenantSettingsService(new LocalMockTenantSettingsRepository(), new LocalMockAesEncryptionService());

        // Seed Classroom & Child
        _classroomRepo.Classrooms.Add(new Classroom
        {
            Id = _classroomId,
            TenantId = _tenantId,
            Name = "Sunflowers",
            AgeGroup = "Pre-K",
            Capacity = 16,
            IsActive = true
        });

        _childRepo.Children.Add(new Child
        {
            Id = _childId,
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            FirstName = "Aarav",
            LastName = "Patel",
            DateOfBirth = new DateTime(2021, 8, 14),
            IsActive = true
        });

        // Seed Active Experience
        _experienceRepo.Experiences.Add(new Experience
        {
            Id = _experienceId,
            TenantId = _tenantId,
            Title = "Clay & Texture Sculpting",
            Description = "Sensory sculpting exploring clay patterns with natural twigs and stamps.",
            Status = "Active"
        });

        // Seed Memories
        _memoryService.Memories.Add(new MemoryEvent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            ChildId = _childId,
            EventType = "Observation",
            Title = "Building clay towers",
            Description = "Aarav spent 20 minutes creating patterned textures on clay cylinders.",
            SourceId = _memoryStoryId,
            SourceType = "child_stories",
            EventDate = DateTimeOffset.UtcNow.AddDays(-5)
        });

        _memoryService.Memories.Add(new MemoryEvent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            ChildId = _childId,
            EventType = "Session",
            Title = "Clay & Texture Sculpting",
            Description = "Participated enthusiastically with peers and shared stamps.",
            SourceId = _memorySessionId,
            SourceType = "experience_participations",
            ExperienceId = _experienceId,
            EventDate = DateTimeOffset.UtcNow.AddDays(-3)
        });

        var httpClient = new HttpClient(new FailingHttpMessageHandler());

        _service = new ChildJourneyInsightService(
            _insightRepo,
            _memoryService,
            _childService,
            _experienceService,
            _settingsService,
            _tenantContext,
            _userContext,
            httpClient,
            NullLogger<ChildJourneyInsightService>.Instance
        );
    }

    [Test]
    public async Task GenerateJourneyInsightsAsync_WithRecordedEvidence_GeneratesGroundedInsights()
    {
        var input = new GenerateJourneyInsightInput
        {
            ChildId = _childId,
            PeriodType = "Last30Days"
        };

        var result = await _service.GenerateJourneyInsightsAsync(input);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ChildId, Is.EqualTo(_childId));
        Assert.That(result.PeriodType, Is.EqualTo("Last30Days"));
        Assert.That(result.EvidenceCount, Is.EqualTo(2));
        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items.Count, Is.GreaterThan(0));

        // Verify categories are present and classified
        var summaryItem = result.Items.FirstOrDefault(i => i.Category == "JourneySummary");
        Assert.That(summaryItem, Is.Not.Null);
        Assert.That(summaryItem!.Classification, Is.EqualTo("RecordedFact"));
        Assert.That(summaryItem.Statement, Does.Contain("Aarav"));

        // Verify persisted
        Assert.That(_insightRepo.Requests.Count, Is.EqualTo(1));
        Assert.That(_insightRepo.Items.Count, Is.EqualTo(result.Items.Count));
    }

    [Test]
    public async Task GenerateJourneyInsightsAsync_PeriodType_CalculatesCorrectDateBounds()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Test Last7Days
        var input7 = new GenerateJourneyInsightInput
        {
            ChildId = _childId,
            PeriodType = "Last7Days"
        };
        var res7 = await _service.GenerateJourneyInsightsAsync(input7);
        Assert.That(res7.PeriodStartDate, Is.EqualTo(today.AddDays(-7)));
        Assert.That(res7.PeriodEndDate, Is.EqualTo(today));

        // Test CurrentMonth
        var inputMonth = new GenerateJourneyInsightInput
        {
            ChildId = _childId,
            PeriodType = "CurrentMonth"
        };
        var resMonth = await _service.GenerateJourneyInsightsAsync(inputMonth);
        Assert.That(resMonth.PeriodStartDate, Is.EqualTo(new DateOnly(today.Year, today.Month, 1)));
    }

    [Test]
    public async Task GenerateJourneyInsightsAsync_EmptyMemories_YieldsTransparentEvidenceGaps()
    {
        // New child with 0 memories
        var newChildId = Guid.NewGuid();
        _childRepo.Children.Add(new Child
        {
            Id = newChildId,
            TenantId = _tenantId,
            ClassroomId = _classroomId,
            FirstName = "Maya",
            LastName = "S",
            DateOfBirth = new DateTime(2022, 1, 1),
            IsActive = true
        });

        var input = new GenerateJourneyInsightInput
        {
            ChildId = newChildId,
            PeriodType = "Last30Days"
        };

        var result = await _service.GenerateJourneyInsightsAsync(input);

        Assert.That(result.EvidenceCount, Is.EqualTo(0));
        Assert.That(result.EvidenceGaps, Is.Not.Null);
        Assert.That(result.EvidenceGaps, Does.Contain("limited"));

        var summary = result.Items.First(i => i.Category == "JourneySummary");
        Assert.That(summary.EvidenceStrength, Is.EqualTo("Limited"));
    }

    [Test]
    public void GenerateJourneyInsightsAsync_UnauthorizedChild_ThrowsArgumentException()
    {
        var alienChildId = Guid.NewGuid(); // Not in repository
        var input = new GenerateJourneyInsightInput
        {
            ChildId = alienChildId,
            PeriodType = "Last30Days"
        };

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _service.GenerateJourneyInsightsAsync(input);
        });
    }

    [Test]
    public void GenerateJourneyInsightsAsync_ChildWithoutClassroom_ThrowsInvalidOperationException()
    {
        var unassignedChildId = Guid.NewGuid();
        _childRepo.Children.Add(new Child
        {
            Id = unassignedChildId,
            TenantId = _tenantId,
            ClassroomId = null, // unassigned
            FirstName = "Noah",
            LastName = "K",
            IsActive = true
        });

        var input = new GenerateJourneyInsightInput
        {
            ChildId = unassignedChildId,
            PeriodType = "Last30Days"
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.GenerateJourneyInsightsAsync(input);
        });
    }

    [Test]
    public async Task GetLatestInsightForChildAsync_ReturnsMostRecentPersistedInsight()
    {
        var input = new GenerateJourneyInsightInput
        {
            ChildId = _childId,
            PeriodType = "Last30Days"
        };
        await _service.GenerateJourneyInsightsAsync(input);

        var latest = await _service.GetLatestInsightForChildAsync(_childId);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.ChildId, Is.EqualTo(_childId));
        Assert.That(latest.Items.Count, Is.GreaterThan(0));
    }
}

// Mock Repository for Child Journey Insights
public class MockChildJourneyInsightRepository : IChildJourneyInsightRepository
{
    public List<ChildJourneyInsightRequest> Requests { get; } = new();
    public List<ChildJourneyInsightItem> Items { get; } = new();

    public Task<ChildJourneyInsightRequest> CreateRequestAsync(ChildJourneyInsightRequest request, IEnumerable<ChildJourneyInsightItem> items, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        Items.AddRange(items);
        request.Items = items.ToList();
        return Task.FromResult(request);
    }

    public Task<ChildJourneyInsightRequest?> GetLatestInsightForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
    {
        var req = Requests.Where(r => r.TenantId == tenantId && r.ChildId == childId)
                          .OrderByDescending(r => r.CreatedAt)
                          .FirstOrDefault();

        if (req != null)
        {
            req.Items = Items.Where(i => i.RequestId == req.Id).ToList();
        }
        return Task.FromResult(req);
    }

    public Task<ChildJourneyInsightRequest?> GetRequestByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var req = Requests.FirstOrDefault(r => r.TenantId == tenantId && r.Id == requestId);
        return Task.FromResult(req);
    }

    public Task<IEnumerable<ChildJourneyInsightItem>> GetItemsByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var items = Items.Where(i => i.TenantId == tenantId && i.RequestId == requestId);
        return Task.FromResult<IEnumerable<ChildJourneyInsightItem>>(items.ToList());
    }
}
