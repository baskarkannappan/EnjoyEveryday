using System.Data;
using EnjoyEveryday.Shared.Data;
using Npgsql;

namespace EnjoyEveryday.Infrastructure.Data;

/// <summary>
/// PostgreSQL connection factory using Npgsql.
/// Creates connections for Dapper queries.
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
