using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Diagnostics;

/// <summary>
/// Microsoft ASP.NET Core IHealthCheck probe for PostgreSQL database connectivity and query responsiveness.
/// </summary>
public class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PostgreSqlHealthCheck> _logger;

    public PostgreSqlHealthCheck(
        IDbConnectionFactory connectionFactory,
        ILogger<PostgreSqlHealthCheck> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            var result = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1;", cancellationToken: cancellationToken));
            sw.Stop();

            if (result == 1)
            {
                return HealthCheckResult.Healthy(
                    description: $"PostgreSQL is responsive ({sw.ElapsedMilliseconds} ms)",
                    data: new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["responseTimeMs"] = sw.ElapsedMilliseconds,
                        ["status"] = "Connected"
                    });
            }

            return HealthCheckResult.Degraded($"PostgreSQL returned unexpected scalar result: {result}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "PostgreSQL health check failed after {ElapsedMs} ms", sw.ElapsedMilliseconds);

            return HealthCheckResult.Unhealthy(
                description: $"PostgreSQL connection failed: {ex.Message}",
                exception: ex,
                data: new System.Collections.Generic.Dictionary<string, object>
                {
                    ["responseTimeMs"] = sw.ElapsedMilliseconds,
                    ["status"] = "Failed"
                });
        }
    }
}
