using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Moq;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class ClassroomReadinessTests
{
    private Guid _tenantId;
    private Guid _orgId;
    private Guid _branchId;
    private Guid _classroomId;

    [SetUp]
    public void SetUp()
    {
        _tenantId = Guid.NewGuid();
        _orgId = Guid.NewGuid();
        _branchId = Guid.NewGuid();
        _classroomId = Guid.NewGuid();
    }

    [Test]
    public async Task Allowed_ValidBranch_ActiveRoom_ActiveLeadTeacher_ActiveTeacher_AvailableCapacity()
    {
        // Arrange
        var mockRepo = new MockReadinessRepository
        {
            BranchData = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 5, assignedTeachers: 1, leadTeachers: 1)
        };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act
        var result = await service.ValidateClassroomReadinessAsync(_tenantId, _branchId, _classroomId);

        // Assert
        Assert.That(result.IsReady, Is.True);
        Assert.That(result.Status, Is.EqualTo("Ready"));
        Assert.That(result.AvailableSpots, Is.EqualTo(10));
        Assert.That(result.MissingRequirements, Is.Empty);
    }

    [Test]
    public async Task Blocked_WhenBranchInactive_ReturnsIsReadyFalse()
    {
        // Arrange
        var data = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 0, assignedTeachers: 1, leadTeachers: 1);
        data.BranchActive = false;
        var mockRepo = new MockReadinessRepository { BranchData = data };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act
        var result = await service.ValidateBranchReadinessAsync(_tenantId, _branchId);

        // Assert
        Assert.That(result.IsReady, Is.False);
        Assert.That(result.BranchActive, Is.False);
        Assert.That(result.MissingRequirements, Has.Some.Contain("branch is currently inactive"));
    }

    [Test]
    public async Task Blocked_WhenBranchHasNoClassrooms_ReturnsIsReadyFalse()
    {
        // Arrange
        var data = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 0, assignedTeachers: 1, leadTeachers: 1);
        data.Classrooms.Clear();
        var mockRepo = new MockReadinessRepository { BranchData = data };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act
        var result = await service.ValidateBranchReadinessAsync(_tenantId, _branchId);

        // Assert
        Assert.That(result.IsReady, Is.False);
        Assert.That(result.HasClassrooms, Is.False);
        Assert.That(result.MissingRequirements, Has.Some.Contain("No classrooms have been created for this branch"));
    }

    [Test]
    public async Task Blocked_WhenBranchHasNoActiveLeadTeacher_ReturnsIsReadyFalse()
    {
        // Arrange
        var data = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 0, assignedTeachers: 1, leadTeachers: 0);
        var mockRepo = new MockReadinessRepository { BranchData = data };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act
        var result = await service.ValidateBranchReadinessAsync(_tenantId, _branchId);

        // Assert
        Assert.That(result.IsReady, Is.False);
        Assert.That(result.HasLeadTeacher, Is.False);
        Assert.That(result.MissingRequirements, Has.Some.Contain("No Lead Teacher has been assigned to this branch"));
    }

    [Test]
    public async Task Blocked_WhenSelectedClassroomInactive_ReturnsIsReadyFalse()
    {
        // Arrange
        var data = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 0, assignedTeachers: 1, leadTeachers: 1);
        data.Classrooms[0].IsActive = false;
        var mockRepo = new MockReadinessRepository { BranchData = data };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act
        var result = await service.ValidateClassroomReadinessAsync(_tenantId, _branchId, _classroomId);

        // Assert
        Assert.That(result.IsReady, Is.False);
        Assert.That(result.ClassroomActive, Is.False);
        Assert.That(result.MissingRequirements, Has.Some.Contain("This classroom is currently inactive"));
    }

    [Test]
    public async Task Blocked_WhenSelectedClassroomHasNoActiveTeacherAssigned_ReturnsIsReadyFalse()
    {
        // Arrange
        var data = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 0, assignedTeachers: 0, leadTeachers: 1);
        var mockRepo = new MockReadinessRepository { BranchData = data };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act
        var result = await service.ValidateClassroomReadinessAsync(_tenantId, _branchId, _classroomId);

        // Assert
        Assert.That(result.IsReady, Is.False);
        Assert.That(result.HasAssignedTeacher, Is.False);
        Assert.That(result.MissingRequirements, Has.Some.Contain("does not have any active Teachers assigned"));
    }

    [Test]
    public async Task Blocked_WhenSelectedClassroomIsFull_ReturnsIsReadyFalseAndStatusFull()
    {
        // Arrange
        var data = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 15, assignedTeachers: 1, leadTeachers: 1);
        var mockRepo = new MockReadinessRepository { BranchData = data };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act
        var result = await service.ValidateClassroomReadinessAsync(_tenantId, _branchId, _classroomId);

        // Assert
        Assert.That(result.IsReady, Is.False);
        Assert.That(result.Status, Is.EqualTo("Full"));
        Assert.That(result.HasAvailableCapacity, Is.False);
        Assert.That(result.AvailableSpots, Is.EqualTo(0));
        Assert.That(result.MissingRequirements, Has.Some.Contain("maximum capacity"));
    }

    [Test]
    public void Blocked_WhenBranchAdminAttemptsToAccessUnauthorizedBranch_ThrowsUnauthorized()
    {
        // Arrange
        var mockRepo = new MockReadinessRepository
        {
            BranchData = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 0, assignedTeachers: 1, leadTeachers: 1)
        };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.BranchAdmin }, OrganizationId = _orgId };
        var scopeMock = new Mock<IAccessScopeRepository>();
        scopeMock.Setup(s => s.GetAssignedBranchIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { Guid.NewGuid() }); // unauthorized branch
        var service = CreateService(mockRepo, userContext, scopeMock.Object);

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await service.ValidateClassroomReadinessAsync(_tenantId, _branchId, _classroomId);
        });
    }

    [Test]
    public async Task Allowed_WhenBranchAdminAuthorizedForBranch_Succeeds()
    {
        // Arrange
        var mockRepo = new MockReadinessRepository
        {
            BranchData = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 0, assignedTeachers: 1, leadTeachers: 1)
        };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.BranchAdmin }, OrganizationId = _orgId };
        var scopeMock = new Mock<IAccessScopeRepository>();
        scopeMock.Setup(s => s.GetAssignedBranchIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { _branchId });
        var service = CreateService(mockRepo, userContext, scopeMock.Object);

        // Act
        var result = await service.ValidateClassroomReadinessAsync(_tenantId, _branchId, _classroomId);

        // Assert
        Assert.That(result.IsReady, Is.True);
    }

    [Test]
    public void EnsureClassroomReadyForEnrollment_WhenNotReady_ThrowsClassroomReadinessException()
    {
        // Arrange
        var data = CreateHealthyBranchData(_branchId, _classroomId, capacity: 15, currentEnrolled: 15, assignedTeachers: 1, leadTeachers: 1);
        var mockRepo = new MockReadinessRepository { BranchData = data };
        var userContext = new MockUserContext { Roles = new[] { RoleTemplates.OrganizationAdmin }, OrganizationId = _orgId };
        var service = CreateService(mockRepo, userContext);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ClassroomReadinessException>(async () =>
        {
            await service.EnsureClassroomReadyForEnrollmentAsync(_tenantId, _branchId, _classroomId);
        });

        Assert.That(ex!.Result.IsReady, Is.False);
        Assert.That(ex.Result.Status, Is.EqualTo("Full"));
    }

    private static BranchReadinessData CreateHealthyBranchData(Guid branchId, Guid classroomId, int capacity, int currentEnrolled, int assignedTeachers, int leadTeachers)
    {
        return new BranchReadinessData
        {
            BranchExists = true,
            BranchName = "Downtown Campus",
            BranchActive = true,
            LeadTeacherCount = leadTeachers,
            Classrooms = new List<ClassroomReadinessData>
            {
                new ClassroomReadinessData
                {
                    Id = classroomId,
                    Name = "Toddler Sunshine",
                    IsActive = true,
                    Status = "Active",
                    Capacity = capacity,
                    CurrentEnrolled = currentEnrolled,
                    AssignedTeacherCount = assignedTeachers
                }
            }
        };
    }

    private ClassroomReadinessService CreateService(
        IClassroomReadinessRepository repo,
        IUserContext userContext,
        IAccessScopeRepository? scopeRepo = null)
    {
        var defaultScopeMock = new Mock<IAccessScopeRepository>();
        defaultScopeMock.Setup(s => s.GetAssignedBranchIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { _branchId });

        return new ClassroomReadinessService(
            repo,
            userContext,
            new MockTenantContext { TenantId = _tenantId },
            new NullResourceAuthorizer(),
            scopeRepo ?? defaultScopeMock.Object);
    }

    private class MockReadinessRepository : IClassroomReadinessRepository
    {
        public BranchReadinessData BranchData { get; set; } = new();

        public Task<BranchReadinessData> GetBranchReadinessDataAsync(Guid tenantId, Guid branchId, CancellationToken ct = default)
        {
            return Task.FromResult(BranchData);
        }
    }

    private class MockUserContext : IUserContext
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = "admin@example.com";
        public bool IsAuthenticated { get; set; } = true;
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
        public Guid? OrganizationId { get; set; }
        public bool HasPermission(string permission) => true;
    }

    private class MockTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = "Tenant";
        public bool IsResolved { get; set; } = true;
    }
}
