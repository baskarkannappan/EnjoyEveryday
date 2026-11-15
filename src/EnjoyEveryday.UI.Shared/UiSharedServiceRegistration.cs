using Microsoft.Extensions.DependencyInjection;
using EnjoyEveryday.UI.Shared.Services;

namespace EnjoyEveryday.UI.Shared;

public static class UiSharedServiceRegistration
{
    public static IServiceCollection AddEnjoyEverydayUiShared(this IServiceCollection services)
    {
        services.AddScoped<IAppNotificationService, AppNotificationService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<CurrencyService>();
        return services;
    }
}
