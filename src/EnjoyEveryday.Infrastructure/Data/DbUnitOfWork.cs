using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EnjoyEveryday.Infrastructure.Data;

/// <summary>
/// Scoped Unit of Work coordinating Dapper transactions on pooled Npgsql connections.
/// </summary>
public sealed class DbUnitOfWork : IDbUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DbUnitOfWork> _logger;
    private IDbConnection? _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public DbUnitOfWork(IDbConnectionFactory connectionFactory, ILogger<DbUnitOfWork> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("Connection has not been initialized. Call BeginTransactionAsync first or obtain an open connection.");
            }
            return _connection;
        }
    }

    public IDbTransaction? CurrentTransaction => _transaction;

    public bool HasActiveTransaction => _transaction != null;

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already active on this Unit of Work.");
        }

        if (_connection == null)
        {
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        }

        if (_connection is DbConnection dbConn)
        {
            _transaction = await dbConn.BeginTransactionAsync(cancellationToken);
        }
        else
        {
            _transaction = (DbTransaction)_connection.BeginTransaction();
        }

        return _transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction exists to commit.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit database transaction.");
            await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while rolling back database transaction.");
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        _connection?.Dispose();

        _transaction = null;
        _connection = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is IAsyncDisposable asyncConn)
        {
            await asyncConn.DisposeAsync();
            _connection = null;
        }
        else if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
