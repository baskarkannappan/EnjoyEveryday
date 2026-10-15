using System;
using System.Threading.Tasks;

namespace EnjoyEveryday.Domain.Repositories;

public interface ITenantSettingsRepository
{
    Task<string?> GetEncryptedAiApiKeyAsync(Guid tenantId);
    Task SetEncryptedAiApiKeyAsync(Guid tenantId, string encryptedKey);
}
