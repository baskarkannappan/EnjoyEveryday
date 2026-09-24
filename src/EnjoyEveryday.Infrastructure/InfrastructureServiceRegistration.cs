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
        services.AddScoped<IExperienceScheduleRepository, ExperienceScheduleRepository>();
        services.AddScoped<IExperienceFeedbackRepository, ExperienceFeedbackRepository>();
        services.AddScoped<IChildStoryRepository, ChildStoryRepository>();

        return services;
    }
}
