using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace EnjoyEveryday.Application.Common.Messaging;

/// <summary>
/// Microsoft IServiceProvider-backed CQRS Dispatcher.
/// Resolves handlers dynamically with zero third-party licensing or external dependencies.
/// Uses a cached method-info lookup to optimize reflection overhead.
/// </summary>
public class MicrosoftServiceProviderDispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, MethodInfo> _queryMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> _commandVoidMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> _commandResultMethodCache = new();

    public MicrosoftServiceProviderDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException($"No query handler registered for query '{queryType.Name}' with result '{typeof(TResult).Name}'.");
        }

        var method = _queryMethodCache.GetOrAdd(queryType, _ =>
            handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))
            ?? throw new InvalidOperationException($"Could not find HandleAsync on '{handlerType.Name}'."));

        var task = (Task<TResult>?)method.Invoke(handler, new object[] { query, cancellationToken });
        if (task == null)
        {
            throw new InvalidOperationException($"Invocation of '{handlerType.Name}.HandleAsync' returned null.");
        }

        return await task.ConfigureAwait(false);
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException($"No command handler registered for command '{commandType.Name}'.");
        }

        var method = _commandVoidMethodCache.GetOrAdd(commandType, _ =>
            handlerType.GetMethod(nameof(ICommandHandler<ICommand>.HandleAsync))
            ?? throw new InvalidOperationException($"Could not find HandleAsync on '{handlerType.Name}'."));

        var task = (Task?)method.Invoke(handler, new object[] { command, cancellationToken });
        if (task == null)
        {
            throw new InvalidOperationException($"Invocation of '{handlerType.Name}.HandleAsync' returned null.");
        }

        await task.ConfigureAwait(false);
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException($"No command handler registered for command '{commandType.Name}' with result '{typeof(TResult).Name}'.");
        }

        var method = _commandResultMethodCache.GetOrAdd(commandType, _ =>
            handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync))
            ?? throw new InvalidOperationException($"Could not find HandleAsync on '{handlerType.Name}'."));

        var task = (Task<TResult>?)method.Invoke(handler, new object[] { command, cancellationToken });
        if (task == null)
        {
            throw new InvalidOperationException($"Invocation of '{handlerType.Name}.HandleAsync' returned null.");
        }

        return await task.ConfigureAwait(false);
    }
}
