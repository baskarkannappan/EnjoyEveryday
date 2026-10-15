using System;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;

namespace EnjoyEveryday.Application.Services;

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
        var encrypted = await _repository.GetEncryptedAiApiKeyAsync(tenantId);
        if (string.IsNullOrEmpty(encrypted)) return null;
        return _encryptionService.Decrypt(encrypted);
    }

    public async Task SetAiApiKeyAsync(Guid tenantId, string apiKey)
    {
        var encrypted = _encryptionService.Encrypt(apiKey);
        await _repository.SetEncryptedAiApiKeyAsync(tenantId, encrypted);
    }
}
