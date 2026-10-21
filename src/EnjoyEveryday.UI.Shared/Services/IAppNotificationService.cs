using System;
using System.Threading.Tasks;

namespace EnjoyEveryday.UI.Shared.Services;

public interface IAppNotificationService
{
    Task ShowSuccessAsync(string message);
    Task ShowErrorAsync(string message);
    Task ShowWarningAsync(string message);
    Task ShowInfoAsync(string message);
    Task HandleExceptionAsync(Exception exception, string? defaultMessage = null);
}
