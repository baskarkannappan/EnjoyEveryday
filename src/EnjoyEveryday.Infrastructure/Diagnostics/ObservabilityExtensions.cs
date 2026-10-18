using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using EnjoyEveryday.Infrastructure.Diagnostics;

namespace EnjoyEveryday.Infrastructure;

public static class ObservabilityExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Registers database health checks and observability services.
    /// </summary>
    public static IServiceCollection AddAppObservability(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "db" });

        return services;
    }

    /// <summary>
    /// Adds ambient correlation ID tracking to the HTTP pipeline.
    /// </summary>
    public static IApplicationBuilder UseAppObservability(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Maps standardized health check endpoints:
    /// - /healthz: Fast liveness check (HTTP 200 Healthy)
    /// - /health: Readiness check validating database connectivity and system status
    /// </summary>
    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness probe (/healthz): instant response, returns 200 OK without touching DB
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, _) =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Healthy");
            }
        }).AllowAnonymous();

        // Readiness probe (/health): full structured JSON response including DB connectivity check
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.ToString(),
                    entries = report.Entries.Select(e => new
                    {
                        key = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.ToString(),
                        error = e.Value.Exception?.Message
                    })
                };

                await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions);
            }
        }).AllowAnonymous();

        return endpoints;
    }
}
