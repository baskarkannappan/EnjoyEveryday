using EnjoyEveryday.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnjoyEveryday.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<OrganizationService>();
        services.AddScoped<BranchService>();
        services.AddScoped<ClassroomService>();
        services.AddScoped<UserService>();
        services.AddScoped<ChildService>();
        services.AddScoped<ExperienceService>();
        services.AddScoped<PlannerService>();
        services.AddScoped<ExecutionService>();
        services.AddScoped<JourneyService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ClassroomContextService>();
        services.AddHttpClient<IAiIdeaService, AiIdeaService>();

        // Register a default mock TenantContext for now until auth is built
        services.AddScoped<EnjoyEveryday.Shared.Tenancy.ITenantContext>(sp => 
            new EnjoyEveryday.Shared.Tenancy.TenantContext 
            { 
                TenantId = Guid.Parse("10000000-0000-0000-0000-000000000001"), 
                TenantName = "Little Stars Daycare" 
            });

        return services;
    }
}
