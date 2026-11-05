using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Services;

/// <summary>
/// Background service that monitors daycare operating hours and automatically
/// performs the end-of-day closure at closing time (7:00 PM / 19:00).
/// Auto-closes any remaining active activities and marks checkout time for open attendances.
/// </summary>
public class DaycareEndOfDayBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DaycareEndOfDayBackgroundService> _logger;
    private readonly TimeProvider _timeProvider;

    public DaycareEndOfDayBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DaycareEndOfDayBackgroundService> logger,
        TimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DaycareEndOfDayBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var localNow = _timeProvider.GetLocalNow();
                // Operating hours end at 7:00 PM (19:00). Closed between 7:00 PM and 7:00 AM.
                bool isDaycareClosed = localNow.Hour >= 19 || localNow.Hour < 7;

                if (isDaycareClosed)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var endOfDayService = scope.ServiceProvider.GetRequiredService<IEndOfDayService>();
                    var today = localNow.Date;

                    var summary = await endOfDayService.GetPendingSummaryAsync(date: today, cancellationToken: stoppingToken);
                    if (summary.TotalPendingCount > 0)
                    {
                        var eodTime = new TimeSpan(19, 0, 0); // 7:00 PM End of Day time
                        var report = await endOfDayService.ExecuteEndOfDayProcessAsync(
                            date: today,
                            checkoutTime: eodTime,
                            cancellationToken: stoppingToken);

                        _logger.LogInformation("Background End of Day Auto-Close executed: {Message}", report.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking or executing automatic end of day closure in background service.");
            }

            // Check every 60 seconds
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

