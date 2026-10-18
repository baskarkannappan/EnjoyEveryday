using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnjoyEveryday.Infrastructure.Diagnostics;

/// <summary>
/// Health check that executes a fast validation query (SELECT 1) against the database
/// to determine database responsiveness.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";

            object? result;
            if (command is DbCommand dbCommand)
            {
                result = await dbCommand.ExecuteScalarAsync(cancellationToken);
            }
            else
            {
                result = command.ExecuteScalar();
            }

            if (result != null && (result.ToString() == "1" || Convert.ToInt32(result) == 1))
            {
                return HealthCheckResult.Healthy("PostgreSQL database connection is alive and responsive.");
            }

            return HealthCheckResult.Degraded("Database connection responded with an unexpected value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL database connection check failed.", ex);
        }
    }
}
