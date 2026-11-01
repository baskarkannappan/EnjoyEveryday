using System;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;

namespace EnjoyEveryday.Application.Services;

public record TenantAiSettingsDto(string? ApiKey, string Provider);

public class TenantSettingsService
{
    private readonly ITenantSettingsRepository _repository;
    private readonly IAesEncryptionService _encryptionService;

    public TenantSettingsService(ITenantSettingsRepository repository, IAesEncryptionService encryptionService)
    {
        _repository = repository;
        _encryptionService = encryptionService;
    }

    public async Task<string?> GetAiApiKeyAsync(Guid tenantId)
    {
        var settings = await GetAiSettingsAsync(tenantId);
        return settings.ApiKey;
    }

    public async Task SetAiApiKeyAsync(Guid tenantId, string apiKey)
    {
        await SetAiSettingsAsync(tenantId, apiKey, "GoogleGemini");
    }

    public async Task<TenantAiSettingsDto> GetAiSettingsAsync(Guid tenantId)
    {
        var (encrypted, provider) = await _repository.GetAiSettingsAsync(tenantId);
        var decrypted = string.IsNullOrEmpty(encrypted) ? null : _encryptionService.Decrypt(encrypted);
        return new TenantAiSettingsDto(decrypted, !string.IsNullOrWhiteSpace(provider) ? provider : "GoogleGemini");
    }

    public async Task SetAiSettingsAsync(Guid tenantId, string apiKey, string provider)
    {
        var encrypted = _encryptionService.Encrypt(apiKey);
        await _repository.SetAiSettingsAsync(tenantId, encrypted, !string.IsNullOrWhiteSpace(provider) ? provider : "GoogleGemini");
    }
}
