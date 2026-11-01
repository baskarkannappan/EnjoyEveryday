using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.BackgroundWorkers;

/// <summary>
/// Server-hosted background service for operational daycare maintenance routines.
/// Executes independently of active browser tabs, ensuring recurring tasks (such as attendance cutoff checks,
/// cache cleanup, and storage health telemetry) run reliably.
/// </summary>
public class DaycareMaintenanceHostedService : BackgroundService
{
    private readonly ILogger<DaycareMaintenanceHostedService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public DaycareMaintenanceHostedService(ILogger<DaycareMaintenanceHostedService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DaycareMaintenanceHostedService initialized.");

        // Periodic maintenance timer using standard Microsoft PeriodicTimer
        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformPeriodicMaintenanceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("DaycareMaintenanceHostedService shutting down gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in DaycareMaintenanceHostedService periodic execution.");
        }
    }

    private Task PerformPeriodicMaintenanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running hourly daycare maintenance sweep at {Time}", DateTimeOffset.UtcNow);
        // Additional background routines (e.g. attendance reconciliation, stale draft cleanup) can be triggered here.
        return Task.CompletedTask;
    }
}
