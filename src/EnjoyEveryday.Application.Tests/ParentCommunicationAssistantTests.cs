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
public class ParentCommunicationAssistantTests
{
    private MockParentCommunicationRepository _commRepo = null!;
    private MockChildRepository _childRepo = null!;
    private MockClassroomRepository _classroomRepo = null!;
    private MockBranchRepository _branchRepo = null!;
    private MockParentRepository _parentRepo = null!;
    private LocalMockExperienceScheduleRepository _scheduleRepo = null!;
    private LocalMockExperienceRepository _experienceRepo = null!;
    private TenantSettingsService _settingsService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private MockAuditService _auditService = null!;
    private ParentCommunicationAssistantService _service = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _classroomId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _guardianPersonId = Guid.NewGuid();
    private readonly Guid _restrictedPersonId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _commRepo = new MockParentCommunicationRepository();
        _childRepo = new MockChildRepository();
        _classroomRepo = new MockClassroomRepository();
        _branchRepo = new MockBranchRepository();
        _parentRepo = new MockParentRepository();
        _scheduleRepo = new LocalMockExperienceScheduleRepository();
        _experienceRepo = new LocalMockExperienceRepository();
        _auditService = new MockAuditService();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "teacher.rachel@enjoyeveryday.test",
            OrganizationId = _orgId
        };

        _settingsService = new TenantSettingsService(new LocalMockTenantSettingsRepository(), new LocalMockAesEncryptionService());

        // Seed Branch & Classroom
        _branchRepo.Branches[_branchId] = new Branch
        {
            Id = _branchId,
            TenantId = _tenantId,
            OrganizationId = _orgId,
            Name = "Downtown Campus"
        };

        _classroomRepo.Classrooms[_classroomId] = new Classroom
        {
            Id = _classroomId,
            TenantId = _tenantId,
            BranchId = _branchId,
            Name = "Bluebirds",
            AgeGroup = "Preschool (3-4)"
        };

        // Seed Child
        _childRepo.Children[_childId] = new Child
        {
            Id = _childId,
            TenantId = _tenantId,
            FirstName = "Aarav",
            LastName = "Patel",
            ClassroomId = _classroomId,
            IsActive = true
        };

        // Seed Guardians
        _commRepo.Recipients[_childId] = new List<ParentCommunicationRecipientInfo>
        {
            new()
            {
                PersonId = _guardianPersonId,
                FullName = "Sunita Patel",
                Relationship = "Mother",
                Email = "sunita.patel@test.com",
                CanReceiveInformation = true,
                ReceivesDailyUpdates = true,
                HasPortalAccess = true
            },
            new()
            {
                PersonId = _restrictedPersonId,
                FullName = "Restricted Relative",
                Relationship = "Relative",
                Email = "restricted@test.com",
                CanReceiveInformation = false,
                ReceivesDailyUpdates = false
            }
        };

        // Seed Attendance
        _parentRepo.Attendances[(_childId, _testDate)] = new ChildAttendance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ChildId = _childId,
            AttendanceDate = _testDate.ToDateTime(TimeOnly.MinValue),
            Status = "Present",
            ArrivalTime = new TimeSpan(8, 30, 0)
        };

        // Seed Approved Moments
        _parentRepo.ApprovedMoments[_childId] = new List<ChildJourneyEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ChildId = _childId,
                Title = "Ladybug Discovery",
                Observation = "Aarav gently watched a ladybug on a leaf and showed it to his friends.",
                ChildVoice = "I found one!",
                ObservationDate = new DateTimeOffset(_testDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                IsParentVisible = true
            }
        };

        // Seed Experience
        var expId = Guid.NewGuid();
        _experienceRepo.Experiences.Add(new Experience
        {
            Id = expId,
            TenantId = _tenantId,
            Title = "Building with Wooden Blocks",
            Description = "Collaborative block architecture exploring balance and symmetry."
        });

        _scheduleRepo.Schedules.Add(new ExperienceSchedule
        {
            Id = Guid.NewGuid(),
            ClassroomId = _classroomId,
            ExperienceId = expId,
            ScheduledDate = _testDate,
            Status = "Completed"
        });

        _service = new ParentCommunicationAssistantService(
            _commRepo,
            _childRepo,
            _classroomRepo,
            _branchRepo,
            _parentRepo,
            _scheduleRepo,
            _experienceRepo,
            _settingsService,
            _tenantContext,
            _userContext,
            _auditService,
            new HttpClient(),
            NullLogger<ParentCommunicationAssistantService>.Instance,
            null);
    }

    private readonly DateOnly _testDate = new DateOnly(2026, 10, 12);

    [Test]
    public async Task GenerateDraftAsync_WithValidEvidence_CreatesPersonalizedDraft()
    {
        var input = new GenerateParentCommunicationInput
        {
            ChildId = _childId,
            ClassroomId = _classroomId,
            CommunicationType = ParentCommunicationTypes.DailyUpdate,
            StartDate = _testDate,
            EndDate = _testDate,
            RecipientPersonId = _guardianPersonId,
            TeacherNotes = "Aarav had a wonderful morning of cooperative play."
        };

        var draft = await _service.GenerateDraftAsync(input);

        Assert.That(draft, Is.Not.Null);
        Assert.That(draft.ChildId, Is.EqualTo(_childId));
        Assert.That(draft.RecipientName, Is.EqualTo("Sunita Patel"));
        Assert.That(draft.Status, Is.EqualTo(ParentCommunicationStatus.Draft));
        Assert.That(draft.Title, Does.Contain("Aarav"));
        Assert.That(draft.ContentFinal, Does.Contain("Dear Sunita Patel"));
        Assert.That(draft.ContentFinal, Does.Contain("Building with Wooden Blocks"));
        Assert.That(draft.ContentFinal, Does.Contain("Ladybug Discovery"));
        Assert.That(draft.FamilyConversationStarter, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateDraftAsync_WhenChildUnauthorized_ThrowsUnauthorizedAccessException()
    {
        var foreignChildId = Guid.NewGuid();
        var input = new GenerateParentCommunicationInput
        {
            ChildId = foreignChildId,
            StartDate = _testDate,
            EndDate = _testDate
        };

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _service.GenerateDraftAsync(input);
        });
    }

    [Test]
    public void GenerateDraftAsync_WhenGuardianHasNoInformationPermission_ThrowsInvalidOperationException()
    {
        var input = new GenerateParentCommunicationInput
        {
            ChildId = _childId,
            RecipientPersonId = _restrictedPersonId,
            StartDate = _testDate,
            EndDate = _testDate
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.GenerateDraftAsync(input);
        });
    }

    [Test]
    public async Task UpdateDraftAsync_ModifiesDraftContentAndFlagsPendingReview()
    {
        var input = new GenerateParentCommunicationInput
        {
            ChildId = _childId,
            StartDate = _testDate,
            EndDate = _testDate
        };
        var draft = await _service.GenerateDraftAsync(input);

        var updateInput = new UpdateParentCommunicationDraftInput
        {
            CommunicationId = draft.Id,
            Title = "Updated: Aarav's Great Day in Bluebirds",
            ContentFinal = "Dear Sunita,\n\nAarav built an amazing bridge with wooden blocks today.\n\nWarmly,\nRachel",
            FamilyConversationStarter = "Ask Aarav how high the bridge was!"
        };

        var updated = await _service.UpdateDraftAsync(updateInput);

        Assert.That(updated.Title, Is.EqualTo("Updated: Aarav's Great Day in Bluebirds"));
        Assert.That(updated.ContentFinal, Does.Contain("built an amazing bridge"));
        Assert.That(updated.Status, Is.EqualTo(ParentCommunicationStatus.PendingReview));
    }

    [Test]
    public async Task ApproveAndSendAsync_RevalidatesRecipientAndMarksSent()
    {
        var input = new GenerateParentCommunicationInput
        {
            ChildId = _childId,
            RecipientPersonId = _guardianPersonId,
            StartDate = _testDate,
            EndDate = _testDate
        };
        var draft = await _service.GenerateDraftAsync(input);

        var sendInput = new SendParentCommunicationInput
        {
            CommunicationId = draft.Id,
            DeliveryChannel = "PortalAndEmail"
        };

        var sent = await _service.ApproveAndSendAsync(sendInput);

        Assert.That(sent.Status, Is.EqualTo(ParentCommunicationStatus.Sent));
        Assert.That(sent.DeliveryStatus, Is.EqualTo("Delivered"));
        Assert.That(sent.SentAt, Is.Not.Null);
        Assert.That(_auditService.Logs.Any(l => l.Action == "ParentCommunication.Sent"), Is.True);
    }

    [Test]
    public async Task ApproveAndSendAsync_IsIdempotent_DoesNotResendIfAlreadySent()
    {
        var input = new GenerateParentCommunicationInput
        {
            ChildId = _childId,
            RecipientPersonId = _guardianPersonId,
            StartDate = _testDate,
            EndDate = _testDate
        };
        var draft = await _service.GenerateDraftAsync(input);

        var sendInput = new SendParentCommunicationInput { CommunicationId = draft.Id };
        var sent1 = await _service.ApproveAndSendAsync(sendInput);
        var initialSentAt = sent1.SentAt;

        var sent2 = await _service.ApproveAndSendAsync(sendInput);

        Assert.That(sent2.Status, Is.EqualTo(ParentCommunicationStatus.Sent));
        Assert.That(sent2.SentAt, Is.EqualTo(initialSentAt));
    }

    // Mocks
    private class MockParentCommunicationRepository : IParentCommunicationRepository
    {
        public Dictionary<Guid, ParentCommunication> Comms = new();
        public Dictionary<Guid, List<ParentCommunicationRecipientInfo>> Recipients = new();

        public Task<ParentCommunication> CreateAsync(ParentCommunication comm, CancellationToken cancellationToken = default)
        {
            Comms[comm.Id] = comm;
            return Task.FromResult(comm);
        }

        public Task UpdateAsync(ParentCommunication comm, CancellationToken cancellationToken = default)
        {
            Comms[comm.Id] = comm;
            return Task.CompletedTask;
        }

        public Task<ParentCommunication?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Comms.TryGetValue(id, out var comm);
            return Task.FromResult(comm?.TenantId == tenantId ? comm : null);
        }

        public Task<IEnumerable<ParentCommunication>> GetByChildIdAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
        {
            var list = Comms.Values.Where(c => c.TenantId == tenantId && c.ChildId == childId).ToList();
            return Task.FromResult<IEnumerable<ParentCommunication>>(list);
        }

        public Task<IEnumerable<ParentCommunicationRecipientInfo>> GetRecipientsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
        {
            Recipients.TryGetValue(childId, out var recs);
            return Task.FromResult<IEnumerable<ParentCommunicationRecipientInfo>>(recs ?? new List<ParentCommunicationRecipientInfo>());
        }

        public Task UpdateDeliveryStatusAsync(Guid tenantId, Guid id, string status, string? error, DateTimeOffset? sentAt, CancellationToken cancellationToken = default)
        {
            if (Comms.TryGetValue(id, out var comm))
            {
                comm.DeliveryStatus = status;
                comm.DeliveryError = error;
                if (sentAt.HasValue) comm.SentAt = sentAt;
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ParentCommunication>> GetRecentCommunicationsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null, int limit = 50, CancellationToken cancellationToken = default)
        {
            var list = Comms.Values.Where(c => c.TenantId == tenantId).Take(limit).ToList();
            return Task.FromResult<IEnumerable<ParentCommunication>>(list);
        }

        public Task<int> GetUnreadCountForEducatorAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null, CancellationToken cancellationToken = default)
        {
            var count = Comms.Values.Count(c => c.TenantId == tenantId && c.DeliveryStatus != "Read");
            return Task.FromResult(count);
        }

        public Task MarkAsReadByEducatorAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
        {
            foreach (var comm in Comms.Values.Where(c => c.TenantId == tenantId && c.ChildId == childId))
            {
                comm.DeliveryStatus = "Read";
            }
            return Task.CompletedTask;
        }
    }

    private class MockChildRepository : IChildRepository
    {
        public Dictionary<Guid, Child> Children = new();

        public Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Children.TryGetValue(id, out var child);
            return Task.FromResult(child?.TenantId == tenantId ? child : null);
        }

        public Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult(Children.Values.Where(c => c.TenantId == tenantId));
        public Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default) => Task.FromResult(Children.Values.Where(c => c.TenantId == tenantId && c.ClassroomId == classroomId));
        public Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default) => Task.FromResult(child);
        public Task UpdateAsync(Child child, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SyncParentGuardianAsync(Guid tenantId, Guid childId, string parentEmail, string parentName, string relationship, string phone, CancellationToken cancellationToken = default) => Task.CompletedTask;
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

    private class MockParentRepository : IParentRepository
    {
        public Dictionary<(Guid, DateOnly), ChildAttendance> Attendances = new();
        public Dictionary<Guid, List<ChildJourneyEntry>> ApprovedMoments = new();

        public Task<ChildAttendance?> GetChildAttendanceForDateAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken cancellationToken = default)
        {
            Attendances.TryGetValue((childId, DateOnly.FromDateTime(date)), out var att);
            return Task.FromResult(att);
        }

        public Task<IEnumerable<ChildJourneyEntry>> GetApprovedJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
        {
            ApprovedMoments.TryGetValue(childId, out var list);
            return Task.FromResult<IEnumerable<ChildJourneyEntry>>(list ?? new List<ChildJourneyEntry>());
        }

        public Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ParentAuthorizedChild>());
        public Task<bool> HasChildAccessAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ChildMilestone>());
        public Task<IEnumerable<Event>> GetApprovedEventsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<Event>());
        public Task<Person?> GetPersonByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<Person?>(null);
        public Task UpdatePersonContactAsync(Guid tenantId, Guid personId, string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
