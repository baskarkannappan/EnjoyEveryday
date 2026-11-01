using System.Data;
using EnjoyEveryday.Shared.Data;
using Npgsql;

namespace EnjoyEveryday.Infrastructure.Data;

/// <summary>
/// PostgreSQL connection factory using NpgsqlDataSource.
/// Manages high-performance connection pooling, prepared statements, and async connections for Dapper queries.
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory, IAsyncDisposable, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly bool _ownsDataSource;

    public NpgsqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlDataSourceBuilder(connectionString);

        // Connection pool & resilience tuning
        builder.ConnectionStringBuilder.Pooling = true;
        if (builder.ConnectionStringBuilder.MinPoolSize == 0)
        {
            builder.ConnectionStringBuilder.MinPoolSize = 5;
        }
        if (builder.ConnectionStringBuilder.MaxPoolSize <= 20)
        {
            builder.ConnectionStringBuilder.MaxPoolSize = 100;
        }
        builder.ConnectionStringBuilder.ConnectionIdleLifetime = 300; // 5 minutes
        builder.ConnectionStringBuilder.Timeout = 15; // 15-second connect timeout
        builder.ConnectionStringBuilder.CommandTimeout = 30; // 30-second default command timeout

        _dataSource = builder.Build();
        _ownsDataSource = true;
    }

    public NpgsqlConnectionFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _ownsDataSource = false;
    }

    /// <summary>
    /// Gets the underlying NpgsqlDataSource instance.
    /// </summary>
    public NpgsqlDataSource DataSource => _dataSource;

    /// <summary>
    /// Creates and opens a pooled database connection asynchronously.
    /// </summary>
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_ownsDataSource)
        {
            _dataSource.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_ownsDataSource)
        {
            await _dataSource.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}

