using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace EnjoyEveryday.UI.Shared.Services;

public class AppNotificationService : IAppNotificationService
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<AppNotificationService> _logger;

    public AppNotificationService(INotificationService notificationService, ILogger<AppNotificationService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ShowSuccessAsync(string message)
    {
        try
        {
            await _notificationService.ShowSuccessToastAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display success toast: {Message}", message);
        }
    }

    public async Task ShowErrorAsync(string message)
    {
        try
        {
            await _notificationService.ShowErrorToastAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display error toast: {Message}", message);
        }
    }

    public async Task ShowWarningAsync(string message)
    {
        try
        {
            await _notificationService.ShowWarningToastAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display warning toast: {Message}", message);
        }
    }

    public async Task ShowInfoAsync(string message)
    {
        try
        {
            await _notificationService.ShowInfoToastAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display info toast: {Message}", message);
        }
    }

    public async Task HandleExceptionAsync(Exception exception, string? defaultMessage = null)
    {
        var actualEx = exception is AggregateException agg && agg.InnerExceptions.Count == 1
            ? agg.InnerExceptions[0]
            : exception;

        // Traverse down wrapper exceptions (like TargetInvocationException) to find real domain/permission/validation error
        while (actualEx.InnerException != null && 
               !(actualEx is UnauthorizedAccessException || actualEx is InvalidOperationException || actualEx is ArgumentException))
        {
            actualEx = actualEx.InnerException;
        }

        var message = !string.IsNullOrWhiteSpace(actualEx.Message) 
            ? actualEx.Message 
            : (defaultMessage ?? "An unexpected error occurred.");

        _logger.LogError(exception, "Handled application exception: {Message}", message);

        if (actualEx is UnauthorizedAccessException)
        {
            var warningText = message.StartsWith("Requires", StringComparison.OrdinalIgnoreCase) || 
                              message.StartsWith("Permission", StringComparison.OrdinalIgnoreCase) ||
                              message.StartsWith("Teacher is not authorized", StringComparison.OrdinalIgnoreCase)
                ? message
                : $"Permission Denied: {message}";

            await ShowWarningAsync(warningText);
        }
        else
        {
            await ShowErrorAsync(message);
        }
    }
}
