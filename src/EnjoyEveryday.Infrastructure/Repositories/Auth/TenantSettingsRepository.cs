using System;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Dapper;

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

    public async Task<(string? EncryptedKey, string? Provider)> GetAiSettingsAsync(Guid tenantId)
    {
        const string sql = "SELECT encrypted_key AS EncryptedKey, ai_provider AS Provider FROM fn_get_tenant_ai_settings(@tenantId);";
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<(string? EncryptedKey, string? Provider)>(
            new Dapper.CommandDefinition(sql, new { tenantId }));
    }

    public async Task SetAiSettingsAsync(Guid tenantId, string encryptedKey, string provider)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteProcedureAsync(
            "sp_set_tenant_ai_settings",
            new { TenantId = tenantId, EncryptedKey = encryptedKey, AiProvider = provider });
    }
}
