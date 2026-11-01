using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Shared.Data;

/// <summary>
/// Scoped Unit of Work interface for coordinating atomic ACID transactions across Dapper operations.
/// </summary>
public interface IDbUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the active database connection associated with this unit of work.
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Gets the active transaction, or null if no transaction has been started.
    /// </summary>
    IDbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Indicates whether a transaction is currently active.
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Begins an atomic database transaction.
    /// </summary>
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the active database transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the active database transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
