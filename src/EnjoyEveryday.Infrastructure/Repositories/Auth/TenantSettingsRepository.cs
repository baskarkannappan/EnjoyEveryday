using System;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class TenantSettingsRepository : ITenantSettingsRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public TenantSettingsRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<string?> GetEncryptedAiApiKeyAsync(Guid tenantId)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarFunctionAsync<string>(
            "fn_get_tenant_encrypted_ai_api_key",
            new { TenantId = tenantId });
    }

    public async Task SetEncryptedAiApiKeyAsync(Guid tenantId, string encryptedKey)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "sp_set_tenant_encrypted_ai_api_key",
            new { TenantId = tenantId, EncryptedKey = encryptedKey });
    }
}
