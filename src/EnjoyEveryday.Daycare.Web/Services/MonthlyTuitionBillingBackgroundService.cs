using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Daycare.Web.Services;

/// <summary>
/// Background service that evaluates monthly tuition billing contracts on the 1st of each month
/// (and checks once daily during early morning hours, e.g., 02:00 AM) to automatically generate
/// recurring monthly tuition invoices for active family contracts with idempotency guarantees.
/// </summary>
public class MonthlyTuitionBillingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonthlyTuitionBillingBackgroundService> _logger;
    private readonly TimeProvider _timeProvider;
    private string? _lastExecutedPeriod;

    public MonthlyTuitionBillingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MonthlyTuitionBillingBackgroundService> logger,
        TimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonthlyTuitionBillingBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var localNow = _timeProvider.GetLocalNow();
                var currentPeriod = localNow.ToString("yyyy-MM");

                // Execute once a month (on the 1st of the month, or initial run for current period)
                // during early morning off-peak hours (between 02:00 and 04:00 AM) or when starting fresh
                bool isEligibleHour = localNow.Hour >= 2 && localNow.Hour <= 4;
                bool isNewPeriod = _lastExecutedPeriod != currentPeriod;

                if (isNewPeriod && (localNow.Day == 1 || isEligibleHour))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
                    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

                    var tenantId = tenantContext.TenantId != Guid.Empty
                        ? tenantContext.TenantId
                        : Guid.Parse("10000000-0000-0000-0000-000000000001");

                    _logger.LogInformation("Executing monthly recurring tuition billing run for tenant {TenantId}, period {Period}", tenantId, currentPeriod);

                    var result = await billingService.ExecuteMonthlyTuitionBillingRunAsync(
                        tenantId,
                        localNow,
                        branchId: null,
                        ct: stoppingToken);

                    _lastExecutedPeriod = currentPeriod;
                    _logger.LogInformation(
                        "Monthly recurring tuition billing completed: {Generated} generated, {Skipped} skipped existing, {Errors} errors, Total \u20b9{Total:N2}",
                        result.InvoicesGenerated,
                        result.SkippedExisting,
                        result.Errors,
                        result.TotalAmountBilled);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing monthly tuition billing run in background service.");
            }

            // Check every 30 minutes
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

