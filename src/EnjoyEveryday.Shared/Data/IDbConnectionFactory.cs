using System.Data;

namespace EnjoyEveryday.Shared.Data;

/// <summary>
/// Factory for creating database connections scoped to the current tenant.
/// All Dapper queries go through this factory.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates an open database connection.
    /// </summary>
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
