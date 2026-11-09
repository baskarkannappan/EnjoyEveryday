using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Audit;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Infrastructure.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnjoyEveryday.Infrastructure;

/// <summary>
/// Registers all Infrastructure services with the DI container.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        var factory = new NpgsqlConnectionFactory(connectionString);
        services.AddSingleton(factory.DataSource);
        services.AddSingleton<IDbConnectionFactory>(factory);
        services.AddScoped<IDbUnitOfWork, DbUnitOfWork>();

        // System Deployment Configuration
        services.AddSingleton<EnjoyEveryday.Application.Services.PlatformAdmin.ISystemConfigurationService, EnjoyEveryday.Infrastructure.Configuration.SystemConfigurationService>();

        // Migrations
        services.AddScoped<SqlMigrationRunner>();

        // Audit
        services.AddScoped<IAuditService, DapperAuditService>();

        // Caching (Tenant-Aware In-Memory Cache)
        services.AddMemoryCache();
        services.AddScoped<EnjoyEveryday.Shared.Caching.ITenantMemoryCache, EnjoyEveryday.Infrastructure.Caching.TenantMemoryCache>();

        // Repositories
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<EnjoyEveryday.Application.Services.IOrganizationHierarchyService, EnjoyEveryday.Infrastructure.Repositories.OrganizationHierarchyService>();
        services.AddScoped<BranchRepository>();
        services.AddScoped<IBranchRepository>(sp => new EnjoyEveryday.Infrastructure.Repositories.Decorators.CachedBranchRepository(
            sp.GetRequiredService<BranchRepository>(),
            sp.GetRequiredService<EnjoyEveryday.Shared.Caching.ITenantMemoryCache>()));
        services.AddScoped<ClassroomRepository>();
        services.AddScoped<IClassroomRepository>(sp => new EnjoyEveryday.Infrastructure.Repositories.Decorators.CachedClassroomRepository(
            sp.GetRequiredService<ClassroomRepository>(),
            sp.GetRequiredService<EnjoyEveryday.Shared.Caching.ITenantMemoryCache>()));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChildRepository, ChildRepository>();
        services.AddScoped<IExperienceRepository, ExperienceRepository>();
        services.AddScoped<IExperienceMarketplaceRepository, ExperienceMarketplaceRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IExperienceScheduleRepository, ExperienceScheduleRepository>();
        services.AddScoped<IExperienceFeedbackRepository, ExperienceFeedbackRepository>();
        services.AddScoped<IChildStoryRepository, ChildStoryRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<IChildJourneyRepository, ChildJourneyRepository>();
        services.AddScoped<IExperienceSessionRepository, ExperienceSessionRepository>();
        services.AddScoped<IHarvestRepository, HarvestRepository>();
        services.AddScoped<ITenantSettingsRepository, TenantSettingsRepository>();
        services.AddScoped<ITenantPreferencesRepository, TenantPreferencesRepository>();
        services.AddScoped<IMasterLookupRepository, MasterLookupRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IEndOfDayRepository, EndOfDayRepository>();
        services.AddScoped<IParentRepository, ParentRepository>();
        services.AddScoped<IAccessScopeRepository, AccessScopeRepository>();
        services.AddScoped<IConsentRepository, ConsentRepository>();
        services.AddScoped<IFloorMapRepository, FloorMapRepository>();
        services.AddScoped<IEmergencyContactRepository, EmergencyContactRepository>();
        services.AddScoped<IChildDailyCareRepository, ChildDailyCareRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IActivityRecommendationRepository, ActivityRecommendationRepository>();
        services.AddScoped<IChildJourneyInsightRepository, ChildJourneyInsightRepository>();
        services.AddScoped<IWeeklyPlanningRepository, WeeklyPlanningRepository>();
        services.AddScoped<IParentCommunicationRepository, ParentCommunicationRepository>();
        services.AddScoped<IOrganizationIntelligenceRepository, OrganizationIntelligenceRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IMaterialMarketplaceRepository, MaterialMarketplaceRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ISecurePickupRepository, SecurePickupRepository>();
        services.AddScoped<IEnrollmentPipelineRepository, EnrollmentPipelineRepository>();
        services.AddScoped<IStaffRosterRepository, StaffRosterRepository>();
        services.AddScoped<IDaycareOperationsBoardRepository, DaycareOperationsBoardRepository>();
        services.AddScoped<IBranchBenchmarkingRepository, BranchBenchmarkingRepository>();
        services.AddScoped<IChildDevelopmentPortfolioRepository, ChildDevelopmentPortfolioRepository>();
        services.AddScoped<IPlatformAdminRepository, PlatformAdminRepository>();
        services.AddScoped<IAgentTokenRepository, EnjoyEveryday.Infrastructure.Repositories.Security.AgentTokenRepository>();
        services.AddScoped<IAgentWebhookRepository, EnjoyEveryday.Infrastructure.Repositories.Security.AgentWebhookRepository>();
        services.AddScoped<IDaycareAnnouncementRepository, DaycareAnnouncementRepository>();

        // Razorpay Payment Gateway
        services.Configure<EnjoyEveryday.Application.Services.RazorpaySettings>(
            configuration.GetSection(EnjoyEveryday.Application.Services.RazorpaySettings.SectionName));
        services.AddHttpClient<EnjoyEveryday.Application.Services.IRazorpayGateway, EnjoyEveryday.Infrastructure.Services.RazorpayGateway>();

        // Realtime Notifications
        services.AddScoped<EnjoyEveryday.Application.Services.IRealtimeNotificationService, EnjoyEveryday.Infrastructure.Services.HttpRealtimeNotificationService>();
        services.AddHttpClient("DaycareRealtimeClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        // Storage (Provider-Independent Object Storage Abstraction + Multi-Provider Adapters)
        services.AddSingleton<EnjoyEveryday.Infrastructure.Storage.LocalDiskObjectStorage>();
        services.AddSingleton<EnjoyEveryday.Domain.Storage.IObjectStorage>(sp => sp.GetRequiredService<EnjoyEveryday.Infrastructure.Storage.LocalDiskObjectStorage>());
        services.AddSingleton<EnjoyEveryday.Domain.Storage.IBlobStorageService>(sp => sp.GetRequiredService<EnjoyEveryday.Infrastructure.Storage.LocalDiskObjectStorage>());

        services.AddSingleton<EnjoyEveryday.Infrastructure.Storage.AzureBlobObjectStorage>();
        services.AddSingleton<EnjoyEveryday.Domain.Storage.IObjectStorage>(sp => sp.GetRequiredService<EnjoyEveryday.Infrastructure.Storage.AzureBlobObjectStorage>());

        services.AddSingleton<EnjoyEveryday.Domain.Storage.IObjectStorage>(sp =>
            new EnjoyEveryday.Infrastructure.Storage.S3CompatibleObjectStorage(
                "AmazonS3",
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                sp.GetRequiredService<EnjoyEveryday.Infrastructure.Storage.LocalDiskObjectStorage>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EnjoyEveryday.Infrastructure.Storage.S3CompatibleObjectStorage>>()));

        services.AddSingleton<EnjoyEveryday.Domain.Storage.IObjectStorage>(sp =>
            new EnjoyEveryday.Infrastructure.Storage.S3CompatibleObjectStorage(
                "CloudflareR2",
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                sp.GetRequiredService<EnjoyEveryday.Infrastructure.Storage.LocalDiskObjectStorage>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EnjoyEveryday.Infrastructure.Storage.S3CompatibleObjectStorage>>()));

        services.AddSingleton<EnjoyEveryday.Domain.Storage.IObjectStorageRegistry, EnjoyEveryday.Infrastructure.Storage.ObjectStorageRegistry>();
        services.AddScoped<EnjoyEveryday.Infrastructure.Storage.StorageMigrationService>();

        // Observability & Diagnostics
        services.AddAppObservability();

        // In-Process Domain Events & Background Processing (Native Microsoft Stack)
        services.AddSingleton<EnjoyEveryday.Infrastructure.Events.InProcessDomainEventChannel>();
        services.AddSingleton<EnjoyEveryday.Shared.Events.IDomainEventPublisher, EnjoyEveryday.Infrastructure.Events.ChannelDomainEventPublisher>();
        services.AddHostedService<EnjoyEveryday.Infrastructure.Events.DomainEventChannelProcessor>();
        services.AddScoped<EnjoyEveryday.Shared.Events.IDomainEventHandler<EnjoyEveryday.Shared.Events.ClassroomMessageSentEvent>,
            EnjoyEveryday.Infrastructure.Events.Handlers.ClassroomMessageBackgroundNotificationHandler>();
        services.AddScoped<EnjoyEveryday.Shared.Events.IDomainEventHandler<EnjoyEveryday.Shared.Events.ClassroomMessageSentEvent>,
            EnjoyEveryday.Infrastructure.Events.Handlers.AgentWebhookNotificationHandler>();
        services.AddScoped<EnjoyEveryday.Shared.Events.IDomainEventHandler<EnjoyEveryday.Shared.Events.ChildAttendanceRecordedEvent>,
            EnjoyEveryday.Infrastructure.Events.Handlers.AgentWebhookNotificationHandler>();
        services.AddHttpClient("AgentWebhookClient");

        // Background Maintenance Worker
        services.AddHostedService<EnjoyEveryday.Infrastructure.BackgroundWorkers.DaycareMaintenanceHostedService>();

        // Health Checks (PostgreSQL Probe)
        services.AddHealthChecks()
            .AddCheck<EnjoyEveryday.Infrastructure.Diagnostics.PostgreSqlHealthCheck>("postgresql", tags: new[] { "ready", "db" });

        return services;
    }
}

