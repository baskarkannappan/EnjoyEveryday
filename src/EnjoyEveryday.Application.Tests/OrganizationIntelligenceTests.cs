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
public class OrganizationIntelligenceTests
{
    private MockOrganizationIntelligenceRepository _intelRepo = null!;
    private MockOrganizationRepository _orgRepo = null!;
    private MockBranchRepository _branchRepo = null!;
    private MockClassroomRepository _classroomRepo = null!;
    private TenantSettingsService _settingsService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private MockAuditService _auditService = null!;
    private OrganizationIntelligenceService _service = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _intelRepo = new MockOrganizationIntelligenceRepository();
        _orgRepo = new MockOrganizationRepository();
        _branchRepo = new MockBranchRepository();
        _classroomRepo = new MockClassroomRepository();
        _auditService = new MockAuditService();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "director.sarah@enjoyeveryday.test",
            OrganizationId = _orgId
        };

        _settingsService = new TenantSettingsService(new LocalMockTenantSettingsRepository(), new LocalMockAesEncryptionService());

        // Seed Org
        _orgRepo.Organizations[_orgId] = new Organization
        {
            Id = _orgId,
            TenantId = _tenantId,
            Name = "Little Stars Early Learning",
            IsActive = true
        };

        // Seed Branch
        _branchRepo.Branches[_branchId] = new Branch
        {
            Id = _branchId,
            TenantId = _tenantId,
            OrganizationId = _orgId,
            Name = "Downtown Branch"
        };

        // Seed Classroom
        _classroomRepo.Classrooms[_classroomId] = new Classroom
        {
            Id = _classroomId,
            TenantId = _tenantId,
            BranchId = _branchId,
            Name = "Bluebirds",
            IsActive = true
        };

        _service = new OrganizationIntelligenceService(
            _intelRepo,
            _orgRepo,
            _branchRepo,
            _classroomRepo,
            _settingsService,
            _tenantContext,
            _userContext,
            _auditService,
            new HttpClient(),
            NullLogger<OrganizationIntelligenceService>.Instance);
    }

    [Test]
    public async Task GetIntelligenceReportAsync_WithValidOrg_ReturnsComprehensiveMetricsAndInsights()
    {
        var filter = new OrganizationIntelligenceFilter
        {
            OrganizationId = _orgId,
            ReportingPeriod = IntelligenceReportingPeriod.Last7Days
        };

        var report = await _service.GetIntelligenceReportAsync(filter);

        Assert.That(report, Is.Not.Null);
        Assert.That(report.OrganizationName, Is.EqualTo("Little Stars Early Learning"));
        Assert.That(report.OperationalOverview, Is.Not.Null);
        Assert.That(report.OperationalOverview.AttendanceRatePercentage, Is.EqualTo(92.5));
        Assert.That(report.OperationalOverview.PlanningCoveragePercentage, Is.EqualTo(80.0));
        Assert.That(report.CurriculumVariety, Is.Not.Null);
        Assert.That(report.CurriculumVariety.VarietyDiversityPercentage, Is.EqualTo(75.0));
        Assert.That(report.Insights, Is.Not.Empty);
        Assert.That(report.Insights.First().EvidenceStrength, Is.EqualTo(EvidenceStrengthLevel.Strong));
    }

    [Test]
    public void GetIntelligenceReportAsync_WithCrossTenantOrg_ThrowsUnauthorizedAccessException()
    {
        var foreignOrgId = Guid.NewGuid();
        var filter = new OrganizationIntelligenceFilter
        {
            OrganizationId = foreignOrgId,
            ReportingPeriod = IntelligenceReportingPeriod.Last7Days
        };

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _service.GetIntelligenceReportAsync(filter);
        });
    }

    [Test]
    public void GetIntelligenceReportAsync_WithMismatchedBranch_ThrowsUnauthorizedAccessException()
    {
        var foreignBranchId = Guid.NewGuid();
        // Seed branch under a different org
        _branchRepo.Branches[foreignBranchId] = new Branch
        {
            Id = foreignBranchId,
            TenantId = _tenantId,
            OrganizationId = Guid.NewGuid(),
            Name = "Other Org Branch"
        };

        var filter = new OrganizationIntelligenceFilter
        {
            OrganizationId = _orgId,
            BranchId = foreignBranchId,
            ReportingPeriod = IntelligenceReportingPeriod.Last7Days
        };

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _service.GetIntelligenceReportAsync(filter);
        });
    }

    [Test]
    public async Task GenerateInsightsAsync_ProducesGroundedInsightsWithAuditLog()
    {
        var filter = new OrganizationIntelligenceFilter
        {
            OrganizationId = _orgId,
            ReportingPeriod = IntelligenceReportingPeriod.Last7Days
        };

        var report = await _service.GenerateInsightsAsync(filter);

        Assert.That(report.Insights, Is.Not.Empty);
        Assert.That(report.Insights.Any(i => i.Category == OrganizationIntelligenceCategory.OperationalOverview), Is.True);
        Assert.That(report.Insights.Any(i => i.Category == OrganizationIntelligenceCategory.CurriculumVariety), Is.True);
        Assert.That(_auditService.Logs.Any(l => l.Action == "OrganizationIntelligence.InsightsGenerated"), Is.True);
    }

    [Test]
    public async Task GetIntelligenceReportAsync_WhenDataCompletenessIsLow_CalculatesInsufficientEvidenceStrength()
    {
        _intelRepo.LowCompletenessMode = true;

        var filter = new OrganizationIntelligenceFilter
        {
            OrganizationId = _orgId,
            ReportingPeriod = IntelligenceReportingPeriod.Last7Days
        };

        var report = await _service.GenerateInsightsAsync(filter);

        Assert.That(report.Insights, Is.Not.Empty);
        Assert.That(report.Insights.First().EvidenceStrength, Is.EqualTo(EvidenceStrengthLevel.Insufficient));
    }

    // Mocks
    private class MockOrganizationIntelligenceRepository : IOrganizationIntelligenceRepository
    {
        public bool LowCompletenessMode { get; set; }
        public List<OrganizationIntelligenceInsight> StoredInsights = new();

        public Task<OperationalOverviewMetrics> GetOperationalOverviewAsync(Guid tenantId, OrganizationIntelligenceFilter filter, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OperationalOverviewMetrics
            {
                ExpectedAttendanceCount = 40,
                ActualPresentCount = 37,
                AbsentCount = 3,
                LateCount = 0,
                AttendanceRatePercentage = 92.5,
                TotalClassrooms = 5,
                PlannedClassroomsCount = 4,
                PlanningCoveragePercentage = 80.0,
                PlannedExperiencesCount = 20,
                CompletedExperiencesCount = 18,
                UpcomingExperiencesCount = 2,
                LoggedMomentsCount = 15,
                LoggedCareEventsCount = 30,
                DataCompletenessScorePercentage = LowCompletenessMode ? 15.0 : 85.0
            });
        }

        public Task<CurriculumVarietyMetrics> GetCurriculumVarietyAsync(Guid tenantId, OrganizationIntelligenceFilter filter, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CurriculumVarietyMetrics
            {
                TotalExperiencesConducted = 20,
                UniqueExperienceTitlesCount = 15,
                VarietyDiversityPercentage = 75.0,
                DomainDistribution = new Dictionary<string, int>
                {
                    ["Creative Expression"] = 6,
                    ["Cognitive & Discovery"] = 5,
                    ["Social-Emotional Play"] = 5,
                    ["Physical & Movement"] = 4
                }
            });
        }

        public Task<PlanningGapsMetrics> GetPlanningGapsAsync(Guid tenantId, OrganizationIntelligenceFilter filter, DateOnly today, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PlanningGapsMetrics
            {
                ClassroomsWithFewOrNoPlans = new List<ClassroomGapItem>
                {
                    new() { ClassroomId = Guid.NewGuid(), ClassroomName = "Robins", BranchName = "Downtown Branch", UpcomingPlansCount = 1 }
                },
                OverdueUncompletedPlansCount = 0
            });
        }

        public Task<DataQualityMetrics> GetDataQualityAsync(Guid tenantId, OrganizationIntelligenceFilter filter, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DataQualityMetrics
            {
                MissingDepartureTimesCount = 2,
                UnmarkedAttendanceDaysCount = 0,
                DraftsAwaitingReviewCount = 1
            });
        }

        public Task<OrganizationIntelligenceInsight> SaveInsightAsync(OrganizationIntelligenceInsight insight, CancellationToken cancellationToken = default)
        {
            StoredInsights.Add(insight);
            return Task.FromResult(insight);
        }

        public Task<IEnumerable<OrganizationIntelligenceInsight>> GetRecentInsightsAsync(Guid tenantId, Guid organizationId, Guid? branchId, Guid? classroomId, string reportingPeriod, CancellationToken cancellationToken = default)
        {
            var list = StoredInsights.Where(i => i.TenantId == tenantId && i.OrganizationId == organizationId).ToList();
            return Task.FromResult<IEnumerable<OrganizationIntelligenceInsight>>(list);
        }
    }

    private class MockOrganizationRepository : IOrganizationRepository
    {
        public Dictionary<Guid, Organization> Organizations = new();

        public Task<Organization?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Organizations.TryGetValue(id, out var org);
            return Task.FromResult(org?.TenantId == tenantId ? org : null);
        }

        public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Organizations.TryGetValue(id, out var org);
            return Task.FromResult(org);
        }

        public Task<IEnumerable<Organization>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(Organizations.Values.Where(o => o.TenantId == tenantId));
        public Task<Organization> AddAsync(Organization org, CancellationToken cancellationToken = default) => Task.FromResult(org);
        public Task UpdateAsync(Organization org, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IEnumerable<Organization>> GetOrganizationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Organization>>(Organizations.Values);
    }

    private class MockBranchRepository : IBranchRepository
    {
        public Dictionary<Guid, Branch> Branches = new();

        public Task<Branch?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Branches.TryGetValue(id, out var b);
            return Task.FromResult(b?.TenantId == tenantId ? b : null);
        }

        public Task<IEnumerable<Branch>> GetByOrganizationIdAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult(Branches.Values.Where(b => b.TenantId == tenantId && b.OrganizationId == organizationId));
        public Task<Branch> AddAsync(Branch branch, CancellationToken cancellationToken = default) => Task.FromResult(branch);
        public Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class MockClassroomRepository : IClassroomRepository
    {
        public Dictionary<Guid, Classroom> Classrooms = new();

        public Task<Classroom?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Classrooms.TryGetValue(id, out var cl);
            return Task.FromResult(cl?.TenantId == tenantId ? cl : null);
        }

        public Task<IEnumerable<Classroom>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(Classrooms.Values.Where(c => c.TenantId == tenantId));
        public Task<IEnumerable<Classroom>> GetByBranchIdAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default) => Task.FromResult(Classrooms.Values.Where(c => c.TenantId == tenantId && c.BranchId == branchId));
        public Task<Classroom> AddAsync(Classroom classroom, CancellationToken cancellationToken = default) => Task.FromResult(classroom);
        public Task UpdateAsync(Classroom classroom, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class MockAuditService : IAuditService
    {
        public List<AuditEntry> Logs = new();
        public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            Logs.Add(entry);
            return Task.CompletedTask;
        }
    }
}
