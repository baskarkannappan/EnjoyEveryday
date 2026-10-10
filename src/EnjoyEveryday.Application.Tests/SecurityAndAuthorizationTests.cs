using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Audit;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests
{
    [TestFixture]
    public class SecurityAndAuthorizationTests
    {
        private class MockUserContext : IUserContext
        {
            public Guid UserId { get; set; } = Guid.NewGuid();
            public string Email { get; set; } = "test@example.com";
            public bool IsAuthenticated { get; set; } = true;
            public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
            public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
            public Guid? OrganizationId { get; set; }

            public bool HasPermission(string permission)
            {
                return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
            }
        }

        private class MockTenantContext : ITenantContext
        {
            public Guid TenantId { get; set; } = Guid.NewGuid();
            public string TenantName { get; set; } = "Test Tenant";
            public bool IsResolved { get; set; } = true;
        }

        private class MockChildRepository : IChildRepository
        {
            public Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default) => Task.FromResult(child);
            public Task UpdateAsync(Child child, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Child?>(new Child { Id = id, TenantId = tenantId });
            public Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Child>>(Array.Empty<Child>());
            public Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Child>>(Array.Empty<Child>());
        }
        
        private class MockUserRepository : IUserRepository
        {
            public Task<User> AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default) => Task.FromResult(user);
            public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<User?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(new User { Id = id, TenantId = tenantId });
            public Task<IEnumerable<User>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<User>>(Array.Empty<User>());
            public Task AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<IEnumerable<User>> GetUsersByRoleAsync(Guid tenantId, string roleName, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<User>>(Array.Empty<User>());
        }

        private class MockAuditService : IAuditService
        {
            public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        [Test]
        public void ChildService_CreateChild_WithoutPermission_ThrowsUnauthorizedAccessException()
        {
            var userContext = new MockUserContext { Permissions = new[] { "some.other.permission" } };
            var tenantContext = new MockTenantContext();
            var service = new ChildService(new MockChildRepository(), tenantContext, new MockAuditService(), userContext);

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateChildAsync("First", "Last", DateTime.UtcNow, Guid.NewGuid());
            });
            
            Assert.That(ex.Message, Does.Contain("Requires child.manage permission."));
        }

        [Test]
        public async Task ChildService_CreateChild_WithPermission_Succeeds()
        {
            var userContext = new MockUserContext { Permissions = new[] { "child.manage" } };
            var tenantContext = new MockTenantContext();
            var service = new ChildService(new MockChildRepository(), tenantContext, new MockAuditService(), userContext);

            var child = await service.CreateChildAsync("First", "Last", DateTime.UtcNow, Guid.NewGuid());
            Assert.That(child, Is.Not.Null);
            Assert.That(child.TenantId, Is.EqualTo(tenantContext.TenantId));
        }

        [Test]
        public void UserService_UpdateUser_CrossTenantAttempt_ThrowsUnauthorizedAccessException()
        {
            var userContext = new MockUserContext { Permissions = new[] { "user.manage" } };
            var tenantContext = new MockTenantContext { TenantId = Guid.NewGuid() };
            var service = new UserService(new MockUserRepository(), tenantContext, userContext);

            var crossTenantUser = new User 
            { 
                Id = Guid.NewGuid(), 
                TenantId = Guid.NewGuid(), // Different tenant!
                FirstName = "Hacker", 
                LastName = "Man" 
            };

            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateUserAsync(crossTenantUser);
            });
            
            Assert.That(ex.Message, Does.Contain("Cross-tenant update attempted."));
        }
    }
}
