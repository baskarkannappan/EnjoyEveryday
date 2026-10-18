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

        services.AddSingleton<IDbConnectionFactory>(
            new NpgsqlConnectionFactory(connectionString));

        // Migrations
        services.AddScoped<SqlMigrationRunner>();

        // Audit
        services.AddScoped<IAuditService, DapperAuditService>();

        // Repositories
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IClassroomRepository, ClassroomRepository>();
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
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IParentRepository, ParentRepository>();
        services.AddScoped<IAccessScopeRepository, AccessScopeRepository>();
        services.AddScoped<IConsentRepository, ConsentRepository>();
        services.AddScoped<IFloorMapRepository, FloorMapRepository>();
        services.AddScoped<IEmergencyContactRepository, EmergencyContactRepository>();
        services.AddScoped<IChildDailyCareRepository, ChildDailyCareRepository>();
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

        // Razorpay Payment Gateway
        services.Configure<EnjoyEveryday.Application.Services.RazorpaySettings>(
            configuration.GetSection(EnjoyEveryday.Application.Services.RazorpaySettings.SectionName));
        services.AddHttpClient<EnjoyEveryday.Application.Services.IRazorpayGateway, EnjoyEveryday.Infrastructure.Services.RazorpayGateway>();

        return services;
    }
}

