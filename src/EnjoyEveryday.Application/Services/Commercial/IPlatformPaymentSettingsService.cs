using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface IPlatformPaymentSettingsService
{
    PlatformPaymentSettings GetCachedSettings();
    Task<PlatformPaymentSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<PlatformPaymentSettings> SaveSettingsAsync(PlatformPaymentSettings settings, string? updatedByEmail = null, CancellationToken ct = default);
}

