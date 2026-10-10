using System;
using System.Collections.Generic;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests
{
    [TestFixture]
    public class AppIdentityContextTests
    {
        private IConfiguration _config;
        private string _originalEnv;

        [SetUp]
        public void Setup()
        {
            _originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var inMemorySettings = new Dictionary<string, string> {
                {"DevTenantId", "10000000-0000-0000-0000-000000000001"},
                {"DevUserId", "50000000-0000-0000-0000-000000000001"}
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _originalEnv);
        }

        [Test]
        public void DevelopmentFallback_CannotAuthenticateUser()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var context = new AppIdentityContext(_config);

            Assert.That(context.IsAuthenticated, Is.False, "Fallback user must never be treated as authenticated.");
        }

        [Test]
        public void UnauthenticatedUser_HasNoApplicationPermissions()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var context = new AppIdentityContext(_config);

            Assert.That(context.Roles, Is.Empty, "Roles must be empty for unauthenticated user.");
            Assert.That(context.Permissions, Is.Empty, "Permissions must be empty for unauthenticated user.");
            Assert.That(context.HasPermission("some.permission"), Is.False, "HasPermission must return false.");
        }

        [Test]
        public void DevelopmentFallbacks_AreDisabledInProduction()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            var context = new AppIdentityContext(_config);

            Assert.That(context.TenantId, Is.EqualTo(Guid.Empty), "TenantId must be empty in Production when unauthenticated.");
            Assert.That(context.UserId, Is.EqualTo(Guid.Empty), "UserId must be empty in Production when unauthenticated.");
        }

        [Test]
        public void BothContextInterfaces_ResolveSameScopedInstance()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_config);
            services.AddScoped<AppIdentityContext>();
            services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AppIdentityContext>());
            services.AddScoped<IUserContext>(sp => sp.GetRequiredService<AppIdentityContext>());
            
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            
            var tenantCtx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            var userCtx = scope.ServiceProvider.GetRequiredService<IUserContext>();
            
            Assert.That(tenantCtx, Is.SameAs(userCtx), "DI should resolve the exact same instance for both ITenantContext and IUserContext.");
        }
    }
}
