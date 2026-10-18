using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class SecurityRemediationRegressionTests
{
    private class DummyHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "EnjoyEveryday";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Test]
    public void ProductionMode_HostHeaderSpoofing_DoesNotEnableDevelopmentIdentity()
    {
        // Arrange: Environment is Production, but an unauthenticated request arrives
        var hostEnv = new DummyHostEnvironment { EnvironmentName = "Production" };
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["DevTenantId"] = "10000000-0000-0000-0000-000000000001",
            ["DevUserId"] = "50000000-0000-0000-0000-000000000002",
            ["DevOrganizationId"] = "20000000-0000-0000-0000-000000000001"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        var httpContextMock = new Mock<HttpContext>();
        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(r => r.Host).Returns(new HostString("localhost", 5000));
        httpContextMock.Setup(c => c.Request).Returns(requestMock.Object);
        httpContextMock.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity())); // Unauthenticated

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        var identityContext = new AppIdentityContext(config, null, httpContextAccessorMock.Object, hostEnv);

        // Assert: Production environment ignores request-controlled host/headers and development fallback is blocked
        Assert.That(identityContext.IsAuthenticated, Is.False, "Must not be treated as authenticated.");
        Assert.That(identityContext.TenantId, Is.EqualTo(Guid.Empty), "TenantId must be empty in Production.");
        Assert.That(identityContext.UserId, Is.EqualTo(Guid.Empty), "UserId must be empty in Production.");
        Assert.That(identityContext.OrganizationId, Is.Null, "OrganizationId must be null in Production.");
    }

    [Test]
    public void HardcodedAdminEmail_DoesNotBypass_ResourceAuthorizerWithoutMembership()
    {
        // Arrange: User has email admin@littlestars.com, but NO active role assignments and NO active organization membership
        var tenantId = Guid.NewGuid();
        var targetOrgId = Guid.NewGuid();
        var normalUserId = Guid.NewGuid();

        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(u => u.IsAuthenticated).Returns(true);
        userContextMock.Setup(u => u.UserId).Returns(normalUserId);
        userContextMock.Setup(u => u.Email).Returns("admin@littlestars.com");
        userContextMock.Setup(u => u.Roles).Returns(Array.Empty<string>());
        userContextMock.Setup(u => u.Permissions).Returns(Array.Empty<string>());
        userContextMock.Setup(u => u.OrganizationId).Returns(targetOrgId);
        userContextMock.Setup(u => u.HasPermission(It.IsAny<string>())).Returns(false);

        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);
        tenantContextMock.Setup(t => t.IsResolved).Returns(true);

        var scopeMock = new Mock<IAccessScopeRepository>();
        // Active membership check returns false (no active membership for this user in target org)
        scopeMock.Setup(s => s.HasActiveMembershipAsync(tenantId, targetOrgId, normalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        scopeMock.Setup(s => s.GetActiveSupportGrantAsync(tenantId, normalUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportGrant?)null);

        var auditMock = new Mock<IAuditService>();

        var authorizer = new ResourceAuthorizer(scopeMock.Object, tenantContextMock.Object, userContextMock.Object, auditMock.Object);

        // Assert: Having admin@littlestars.com does NOT bypass authorization or membership checks
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await authorizer.RequireOrganizationAsync(targetOrgId, Permissions.OrganizationManage);
        });
    }

    [Test]
    public void AccountBookService_MissingBillingPermissions_ThrowsUnauthorized()
    {
        // Arrange: User is authenticated but holds neither PlatformBillingManage nor OrganizationBillingView
        var userMock = new Mock<IUserContext>();
        userMock.Setup(u => u.IsAuthenticated).Returns(true);
        userMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        userMock.Setup(u => u.Roles).Returns(new[] { "DaycareTeacher" });
        userMock.Setup(u => u.Permissions).Returns(new[] { Permissions.AttendanceView });
        userMock.Setup(u => u.HasPermission(It.IsAny<string>())).Returns(false);

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(Guid.NewGuid());

        var service = new AccountBookService(
            Mock.Of<IPlatformAdminService>(),
            Mock.Of<IBillingRepository>(),
            Mock.Of<IMaterialMarketplaceRepository>(),
            Mock.Of<IOrganizationRepository>(),
            userMock.Object,
            tenantMock.Object);

        // Assert: Calling financial overview throws UnauthorizedAccessException
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await service.GetFinancialOverviewAsync();
        });
    }

    [Test]
    public void AccountBookService_CrossTenantInvestigation_DoesNotExposeOtherOrgTransactions()
    {
        // Arrange: User belongs to Org A (has OrganizationBillingView), but attempts to view transaction from Org B
        var orgA = Guid.Parse("20000000-0000-0000-0000-000000000001");

        var userMock = new Mock<IUserContext>();
        userMock.Setup(u => u.IsAuthenticated).Returns(true);
        userMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        userMock.Setup(u => u.OrganizationId).Returns(orgA);
        userMock.Setup(u => u.Roles).Returns(new[] { RoleTemplates.OrganizationOwner });
        userMock.Setup(u => u.HasPermission(Permissions.OrganizationBillingView)).Returns(true);

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(Guid.NewGuid());

        var service = new AccountBookService(
            Mock.Of<IPlatformAdminService>(),
            Mock.Of<IBillingRepository>(),
            Mock.Of<IMaterialMarketplaceRepository>(),
            Mock.Of<IOrganizationRepository>(),
            userMock.Object,
            tenantMock.Object);

        // Assert: MAT-00821 belongs to Org 2. Org 1 user must receive null (not exposed)
        Assert.DoesNotThrowAsync(async () =>
        {
            var result = await service.GetInvestigationDetailsAsync("MAT-00821");
            Assert.That(result, Is.Null, "Transactions belonging to another organization must not be exposed to Org A.");
        });
    }

    [Test]
    public async Task AttendanceService_GetPresentChildIdsForClassroom_PassesTenantId()
    {
        // Arrange: Verify that AttendanceService enforces tenant scoping on classroom attendance query
        var tenantId = Guid.NewGuid();
        var classroomId = Guid.NewGuid();
        var expectedChildId = Guid.NewGuid();

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(tenantId);

        var attendanceRepoMock = new Mock<IAttendanceRepository>();
        attendanceRepoMock.Setup(r => r.GetPresentChildIdsByClassroomAsync(tenantId, classroomId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { expectedChildId });

        var service = new AttendanceService(attendanceRepoMock.Object, Mock.Of<IClassroomRepository>(), tenantMock.Object);

        // Act
        var presentIds = await service.GetPresentChildIdsForClassroomAsync(classroomId, DateTime.UtcNow.Date);

        // Assert
        Assert.That(presentIds, Contains.Item(expectedChildId));
        attendanceRepoMock.Verify(r => r.GetPresentChildIdsByClassroomAsync(tenantId, classroomId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
