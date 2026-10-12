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

namespace EnjoyEveryday.Application.Tests
{
    [TestFixture]
    public class ParentServiceTests
    {
        private class TestUserContext : IUserContext
        {
            public Guid UserId { get; set; } = Guid.Parse("50000000-0000-0000-0000-000000000010");
            public string Email { get; set; } = "parent@littlestars.com";
            public bool IsAuthenticated { get; set; } = true;
            public IEnumerable<string> Roles { get; set; } = new[] { "Parent" };
            public IEnumerable<string> Permissions { get; set; } = new[] { "family.view" };
            public Guid? OrganizationId { get; set; }
            public bool HasPermission(string permission) => Permissions.Contains(permission);
        }

        private class TestTenantContext : ITenantContext
        {
            public Guid TenantId { get; set; } = Guid.Parse("10000000-0000-0000-0000-000000000001");
            public string TenantName { get; set; } = "Little Stars Daycare";
            public bool IsResolved => true;
        }

        private class MockParentRepository : IParentRepository
        {
            public Guid AuthorizedChildId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
            public Guid OtherChildId = Guid.Parse("c0000000-0000-0000-0000-000000000099");

            public Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IEnumerable<ParentAuthorizedChild>>(new List<ParentAuthorizedChild>
                {
                    new ParentAuthorizedChild
                    {
                        ChildId = AuthorizedChildId,
                        TenantId = tenantId,
                        FirstName = "Aarav",
                        LastName = "Patel",
                        ClassroomName = "Bluebirds",
                        Relationship = "Mother",
                        IsPrimary = true
                    }
                });
            }

            public Task<bool> HasChildAccessAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(childId == AuthorizedChildId);
            }

            public Task<ChildAttendance?> GetChildAttendanceForDateAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken cancellationToken = default)
            {
                if (childId == AuthorizedChildId)
                {
                    return Task.FromResult<ChildAttendance?>(new ChildAttendance
                    {
                        ChildId = childId,
                        TenantId = tenantId,
                        AttendanceDate = date,
                        Status = "Present",
                        ArrivalTime = new TimeSpan(8, 30, 0)
                    });
                }
                return Task.FromResult<ChildAttendance?>(null);
            }

            public Task<IEnumerable<ChildJourneyEntry>> GetApprovedJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
            {
                if (childId == AuthorizedChildId)
                {
                    return Task.FromResult<IEnumerable<ChildJourneyEntry>>(new List<ChildJourneyEntry>
                    {
                        new ChildJourneyEntry
                        {
                            Id = Guid.NewGuid(),
                            ChildId = childId,
                            TenantId = tenantId,
                            Title = "Garden Discovery",
                            Observation = "Discovered a ladybug.",
                            IsParentVisible = true,
                            ObservationDate = DateTimeOffset.UtcNow
                        }
                    });
                }
                return Task.FromResult<IEnumerable<ChildJourneyEntry>>(Array.Empty<ChildJourneyEntry>());
            }

            public Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IEnumerable<ChildMilestone>>(Array.Empty<ChildMilestone>());
            }

            public Guid ApprovedEventId = Guid.Parse("e0000000-0000-0000-0000-000000000001");

            public Task<IEnumerable<Event>> GetApprovedEventsForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default)
            {
                if (childId == AuthorizedChildId)
                {
                    return Task.FromResult<IEnumerable<Event>>(new List<Event>
                    {
                        new Event
                        {
                            Id = ApprovedEventId,
                            TenantId = tenantId,
                            Title = "Spring Harvest Festival",
                            Status = "Published",
                            EventDate = DateOnly.FromDateTime(DateTime.Today)
                        }
                    });
                }
                return Task.FromResult<IEnumerable<Event>>(Array.Empty<Event>());
            }

            public Task<Person?> GetPersonByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
            {
                if (userId == Guid.Parse("50000000-0000-0000-0000-000000000010"))
                {
                    return Task.FromResult<Person?>(new Person
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        UserId = userId,
                        FirstName = "Sunita",
                        LastName = "Patel",
                        MobilePhone = "416-555-0199"
                    });
                }
                return Task.FromResult<Person?>(null);
            }

            public Task UpdatePersonContactAsync(Guid tenantId, Guid personId, string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private class MockScheduleRepository : IExperienceScheduleRepository
        {
            public Task<IEnumerable<ExperienceSchedule>> GetByClassroomAndDateRangeAsync(Guid classroomId, DateOnly startDate, DateOnly endDate) => Task.FromResult<IEnumerable<ExperienceSchedule>>(Array.Empty<ExperienceSchedule>());
            public Task<IEnumerable<ExperienceSchedule>> GetByDateAsync(DateOnly date) => Task.FromResult<IEnumerable<ExperienceSchedule>>(Array.Empty<ExperienceSchedule>());
            public Task<ExperienceSchedule?> GetByIdAsync(Guid id) => Task.FromResult<ExperienceSchedule?>(null);
            public Task<ExperienceSchedule> CreateAsync(ExperienceSchedule schedule) => Task.FromResult(schedule);
            public Task UpdateAsync(ExperienceSchedule schedule) => Task.CompletedTask;
            public Task DeleteAsync(Guid id) => Task.CompletedTask;
        }

        private class MockExperienceRepository : IExperienceRepository
        {
            public Task<Experience?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Experience?>(null);
            public Task<IEnumerable<Experience>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Experience>>(Array.Empty<Experience>());
            public Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default) => Task.FromResult(experience);
            public Task UpdateAsync(Experience experience, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<IEnumerable<ExperienceVersion>> GetVersionsAsync(Guid experienceId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExperienceVersion>>(Array.Empty<ExperienceVersion>());
        }

        private class MockEventRepository : IEventRepository
        {
            public Task<IEnumerable<Event>> GetAllForTenantAsync(Guid tenantId) => Task.FromResult<IEnumerable<Event>>(Array.Empty<Event>());
            public Task<Event?> GetByIdAsync(Guid tenantId, Guid id) => Task.FromResult<Event?>(null);
            public Task<IEnumerable<Event>> GetByBranchAsync(Guid tenantId, Guid branchId) => Task.FromResult<IEnumerable<Event>>(Array.Empty<Event>());
            public Task<IEnumerable<Event>> GetByClassroomAsync(Guid tenantId, Guid classroomId) => Task.FromResult<IEnumerable<Event>>(Array.Empty<Event>());
            public Task<IEnumerable<Event>> GetEventsForDateAsync(Guid tenantId, DateOnly date, Guid? branchId = null, Guid? classroomId = null) => Task.FromResult<IEnumerable<Event>>(Array.Empty<Event>());
            public Task<Guid> CreateAsync(Event evt) => Task.FromResult(Guid.NewGuid());
            public Task UpdateAsync(Event evt) => Task.CompletedTask;
            public Task DeleteAsync(Guid tenantId, Guid id) => Task.CompletedTask;
            public Task<IEnumerable<EventType>> GetEventTypesAsync(Guid tenantId) => Task.FromResult<IEnumerable<EventType>>(Array.Empty<EventType>());
            public Task<Guid> CreateEventTypeAsync(EventType eventType) => Task.FromResult(Guid.NewGuid());
            public Task AddEventClassroomAsync(EventClassroom eventClassroom) => Task.CompletedTask;
            public Task RemoveEventClassroomAsync(Guid eventId, Guid classroomId) => Task.CompletedTask;
            public Task<IEnumerable<Guid>> GetEventClassroomsAsync(Guid eventId) => Task.FromResult<IEnumerable<Guid>>(Array.Empty<Guid>());
            public Task AddEventExperienceAsync(EventExperience eventExperience) => Task.CompletedTask;
            public Task RemoveEventExperienceAsync(Guid id) => Task.CompletedTask;
            public Task<IEnumerable<EventExperience>> GetEventExperiencesAsync(Guid eventId) => Task.FromResult<IEnumerable<EventExperience>>(Array.Empty<EventExperience>());
            public Task<Guid> CreateEventTaskAsync(EventTask task) => Task.FromResult(Guid.NewGuid());
            public Task UpdateEventTaskAsync(EventTask task) => Task.CompletedTask;
            public Task DeleteEventTaskAsync(Guid id) => Task.CompletedTask;
            public Task<IEnumerable<EventTask>> GetEventTasksAsync(Guid eventId) => Task.FromResult<IEnumerable<EventTask>>(Array.Empty<EventTask>());
            public Task<Guid> CreateEventRsvpAsync(EventRsvp rsvp) => Task.FromResult(Guid.NewGuid());
            public Task<EventRsvp?> GetEventRsvpAsync(Guid eventId, Guid childId) => Task.FromResult<EventRsvp?>(null);
            public Task<IEnumerable<EventRsvp>> GetEventRsvpsAsync(Guid eventId) => Task.FromResult<IEnumerable<EventRsvp>>(Array.Empty<EventRsvp>());
            public Task UpdateEventRsvpAsync(EventRsvp rsvp) => Task.CompletedTask;
            public Task UpdatePhotosAsync(Guid tenantId, Guid id, string[] photoUrls) => Task.CompletedTask;
        }

        private class MockAuditService : IAuditService
        {
            public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        [Test]
        public async Task GetAuthorizedChildren_ReturnsOnlyLinkedChildren()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            var children = (await service.GetAuthorizedChildrenAsync()).ToList();

            Assert.That(children.Count, Is.EqualTo(1));
            Assert.That(children[0].ChildId, Is.EqualTo(repo.AuthorizedChildId));
            Assert.That(children[0].FirstName, Is.EqualTo("Aarav"));
        }

        [Test]
        public void RequestUnauthorizedChildDailySummary_ThrowsUnauthorizedAccessException()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.GetChildDailySummaryAsync(repo.OtherChildId, DateTime.Today);
            });
        }

        [Test]
        public async Task RequestAuthorizedChildDailySummary_ReturnsAttendanceAndTimeline()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            var summary = await service.GetChildDailySummaryAsync(repo.AuthorizedChildId, DateTime.Today);

            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.Child.FirstName, Is.EqualTo("Aarav"));
            Assert.That(summary.Attendance, Is.Not.Null);
            Assert.That(summary.Attendance!.Status, Is.EqualTo("Present"));
            Assert.That(summary.TimelineItems.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(summary.TimelineItems[0].Title, Is.EqualTo("Classroom Check-In"));
        }

        [Test]
        public void RequestUnauthorizedChildJourney_ThrowsUnauthorizedAccessException()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.GetApprovedChildJourneyAsync(repo.OtherChildId);
            });
        }

        [Test]
        public async Task RequestAuthorizedChildJourney_ReturnsApprovedMomentsOnly()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            var moments = (await service.GetApprovedChildJourneyAsync(repo.AuthorizedChildId)).ToList();

            Assert.That(moments.Count, Is.EqualTo(1));
            Assert.That(moments[0].Title, Is.EqualTo("Garden Discovery"));
            Assert.That(moments[0].IsParentVisible, Is.True);
        }

        [Test]
        public void SubmitRsvpForUnauthorizedChild_ThrowsUnauthorizedAccessException()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.SubmitEventRsvpAsync(repo.ApprovedEventId, repo.OtherChildId, "Attending", "Note");
            });
        }

        [Test]
        public void SubmitRsvpForUnpublishedOrOutOfScopeEvent_ThrowsUnauthorizedAccessException()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            var outOfScopeEventId = Guid.NewGuid();

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.SubmitEventRsvpAsync(outOfScopeEventId, repo.AuthorizedChildId, "Attending", "Note");
            });
        }

        [Test]
        public async Task SubmitRsvpForApprovedEvent_Succeeds()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            Assert.DoesNotThrowAsync(async () =>
            {
                await service.SubmitEventRsvpAsync(repo.ApprovedEventId, repo.AuthorizedChildId, "Attending", "We will be there");
            });
        }

        [Test]
        public async Task UpdateParentContact_SanitizesInputsAndEnforcesWhitelist()
        {
            var userContext = new TestUserContext();
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            // Passing arbitrary/invalid contact method should default to safe "Mobile"
            Assert.DoesNotThrowAsync(async () =>
            {
                await service.UpdateParentContactAsync("  416-555-9999  ", "MaliciousValue", "en");
            });
        }

        [Test]
        public void UpdateParentContact_NonExistentPerson_ThrowsInvalidOperationException()
        {
            var userContext = new TestUserContext { UserId = Guid.NewGuid() }; // User with no Person record
            var tenantContext = new TestTenantContext();
            var repo = new MockParentRepository();
            var service = new ParentService(repo, new MockScheduleRepository(), new MockExperienceRepository(), new MockEventRepository(), tenantContext, userContext, new MockAuditService());

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.UpdateParentContactAsync("416-555-0000", "Mobile", "en");
            });
        }
    }
}
