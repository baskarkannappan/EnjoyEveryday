using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace EnjoyEveryday.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<OrganizationService>();
        services.AddScoped<BranchService>();
        services.AddScoped<SystemProvisioningService>();
        services.AddScoped<ClassroomService>();
        services.AddScoped<UserService>();
        services.AddScoped<ChildService>();
        services.AddScoped<ExperienceService>();
        services.AddScoped<ExperienceCurationService>();
        services.AddScoped<ExperienceMarketplaceService>();
        services.AddScoped<PlannerService>();
        services.AddScoped<ExecutionService>();
        services.AddScoped<EventService>();
        services.AddScoped<HarvestService>();
        services.AddScoped<JourneyService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<IDaycareOperationsBoardService, DaycareOperationsBoardService>();
        services.AddScoped<DaycareOperationsBoardService>();
        services.AddScoped<ClassroomContextService>();
        services.AddScoped<FeedbackService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IChildJourneyService, ChildJourneyService>();
        services.AddScoped<IChildDevelopmentPortfolioService, ChildDevelopmentPortfolioService>();
        services.AddScoped<ChildDevelopmentPortfolioService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IAesEncryptionService, AesEncryptionService>();
        services.AddScoped<TenantSettingsService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<ISecurePickupService, SecurePickupService>();
        services.AddScoped<SecurePickupService>();
        services.AddScoped<IStaffRosterService, StaffRosterService>();
        services.AddScoped<StaffRosterService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<ParentService>();
        services.AddSingleton<IConsentStorageService, LocalConsentStorageService>();
        services.AddScoped<IConsentEmailService, ConsentEmailService>();
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<ConsentService>();
        services.AddSingleton<IFloorMapStorageService, LocalFloorMapStorageService>();
        services.AddScoped<IFloorMapService, FloorMapService>();
        services.AddScoped<FloorMapService>();
        services.AddScoped<IEmergencyContactService, EmergencyContactService>();
        services.AddScoped<EmergencyContactService>();
        services.AddScoped<IChildDailyCareService, ChildDailyCareService>();
        services.AddScoped<ChildDailyCareService>();
        services.AddScoped<IEndOfDayService, EndOfDayService>();
        services.AddScoped<EndOfDayService>();
        Action<HttpClient> configureAiClient = client =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:8000");
            client.Timeout = TimeSpan.FromSeconds(15);
        };

        services.AddHttpClient<IAiIdeaService, AiIdeaService>(configureAiClient);
        services.AddHttpClient<IActivityRecommendationService, ActivityRecommendationService>(configureAiClient);
        services.AddScoped<ActivityRecommendationService>();
        services.AddHttpClient<IChildJourneyInsightService, ChildJourneyInsightService>(configureAiClient);
        services.AddScoped<ChildJourneyInsightService>();
        services.AddHttpClient<IWeeklyPlanningAssistantService, WeeklyPlanningAssistantService>(configureAiClient);
        services.AddScoped<WeeklyPlanningAssistantService>();
        services.AddHttpClient<IParentCommunicationAssistantService, ParentCommunicationAssistantService>(configureAiClient);
        services.AddScoped<ParentCommunicationAssistantService>();
        services.AddHttpClient<IOrganizationIntelligenceService, OrganizationIntelligenceService>(configureAiClient);
        services.AddScoped<OrganizationIntelligenceService>();
        services.AddScoped<IPlatformAdminService, PlatformAdminService>();
        services.AddScoped<PlatformAdminService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<BillingService>();
        services.AddScoped<IMaterialMarketplaceService, MaterialMarketplaceService>();
        services.AddScoped<MaterialMarketplaceService>();
        services.AddScoped<IAccountBookService, AccountBookService>();
        services.AddScoped<AccountBookService>();
        services.AddScoped<IEnrollmentPipelineService, EnrollmentPipelineService>();
        services.AddScoped<EnrollmentPipelineService>();
        services.AddScoped<IBranchBenchmarkingService, BranchBenchmarkingService>();
        services.AddScoped<BranchBenchmarkingService>();

        // Register the dynamic IdentityContext for both abstractions
        services.AddScoped<AppIdentityContext>(sp => new AppIdentityContext(
            sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
            sp,
            sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            sp.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>()));
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AppIdentityContext>());
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<AppIdentityContext>());
        services.AddScoped<IResourceAuthorizer, ResourceAuthorizer>();

        return services;
    }
}
