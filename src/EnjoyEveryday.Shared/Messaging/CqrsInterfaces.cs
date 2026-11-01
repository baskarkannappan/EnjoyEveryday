using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Shared.Messaging;

/// <summary>
/// Marker interface for a CQRS Query that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
public interface IQuery<out TResult>
{
}

/// <summary>
/// Handler contract for executing a query.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for a CQRS Command without a return value.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface for a CQRS Command with a return value of type <typeparamref name="TResult"/>.
/// </summary>
public interface ICommand<out TResult>
{
}

/// <summary>
/// Handler contract for executing a void command.
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler contract for executing a command that yields a result.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Native Microsoft-powered CQRS Dispatcher contract.
/// Dispatches queries and commands through Microsoft.Extensions.DependencyInjection without third-party libraries.
/// </summary>
public interface IDispatcher
{
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
