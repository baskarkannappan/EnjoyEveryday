using System;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Repositories;
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
        var sql = "SELECT encrypted_ai_api_key FROM tenants WHERE id = @TenantId";
        return await connection.QuerySingleOrDefaultAsync<string>(sql, new { TenantId = tenantId });
    }

    public async Task SetEncryptedAiApiKeyAsync(Guid tenantId, string encryptedKey)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var sql = "UPDATE tenants SET encrypted_ai_api_key = @EncryptedKey WHERE id = @TenantId";
        await connection.ExecuteAsync(sql, new { TenantId = tenantId, EncryptedKey = encryptedKey });
    }
}
