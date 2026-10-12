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
        services.AddScoped<PlannerService>();
        services.AddScoped<ExecutionService>();
        services.AddScoped<EventService>();
        services.AddScoped<HarvestService>();
        services.AddScoped<JourneyService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ClassroomContextService>();
        services.AddScoped<FeedbackService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IChildJourneyService, ChildJourneyService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IAesEncryptionService, AesEncryptionService>();
        services.AddScoped<TenantSettingsService>();
        services.AddScoped<AttendanceService>();
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
        services.AddHttpClient<IAiIdeaService, AiIdeaService>();
        services.AddHttpClient<IActivityRecommendationService, ActivityRecommendationService>();
        services.AddScoped<ActivityRecommendationService>();
        services.AddHttpClient<IChildJourneyInsightService, ChildJourneyInsightService>();
        services.AddScoped<ChildJourneyInsightService>();

        // Register the dynamic IdentityContext for both abstractions
        services.AddScoped<AppIdentityContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AppIdentityContext>());
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<AppIdentityContext>());

        return services;
    }
}
