using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Events;

/// <summary>
/// Long-running Microsoft BackgroundService that processes asynchronous domain events from InProcessDomainEventChannel.
/// Ensures failures in background handlers do not impact the producer web requests or crash the worker.
/// </summary>
public class DomainEventChannelProcessor : BackgroundService
{
    private readonly InProcessDomainEventChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DomainEventChannelProcessor> _logger;
    private static readonly ConcurrentDictionary<Type, MethodInfo> _handlerMethodCache = new();

    public DomainEventChannelProcessor(
        InProcessDomainEventChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<DomainEventChannelProcessor> logger)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DomainEventChannelProcessor started listening for background events.");

        try
        {
            await foreach (var domainEvent in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessEventAsync(domainEvent, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("DomainEventChannelProcessor cancellation requested, shutting down gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error occurred in DomainEventChannelProcessor read loop.");
        }
    }

    private async Task ProcessEventAsync(IDomainEvent domainEvent, CancellationToken stoppingToken)
    {
        var eventType = domainEvent.GetType();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var method = _handlerMethodCache.GetOrAdd(eventType, t =>
                    handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
                    ?? throw new InvalidOperationException($"Could not find HandleAsync on {handlerType.Name}"));

                try
                {
                    var task = (Task?)method.Invoke(handler, new object[] { domainEvent, stoppingToken });
                    if (task != null)
                    {
                        await task.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling domain event {EventType} by handler {HandlerType}",
                        eventType.Name, handler.GetType().Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing domain event {EventType}", eventType.Name);
        }
    }
}
