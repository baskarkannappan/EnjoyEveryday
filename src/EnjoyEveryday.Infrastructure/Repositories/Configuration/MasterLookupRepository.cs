using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class MasterLookupRepository : IMasterLookupRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public MasterLookupRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IReadOnlyList<MasterLookupItem>> GetActiveLookupsAsync(string? category = null, CancellationToken ct = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id, category, code, display_name AS DisplayName,
                   display_order AS DisplayOrder, is_active AS IsActive,
                   metadata_json::text AS MetadataJson
            FROM public.fn_get_active_master_lookups(@Category);";

        var items = await connection.QueryAsync<MasterLookupItem>(
            new CommandDefinition(sql, new { Category = category }, cancellationToken: ct));

        return items.ToList();
    }
}
