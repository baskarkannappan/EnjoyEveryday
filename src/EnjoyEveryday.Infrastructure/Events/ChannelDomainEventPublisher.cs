using System;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Events;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Events;

/// <summary>
/// Publishes domain events to the in-process System.Threading.Channels queue.
/// Returns immediately without blocking the active request pipeline.
/// </summary>
public class ChannelDomainEventPublisher : IDomainEventPublisher
{
    private readonly InProcessDomainEventChannel _channel;
    private readonly ILogger<ChannelDomainEventPublisher> _logger;

    public ChannelDomainEventPublisher(
        InProcessDomainEventChannel channel,
        ILogger<ChannelDomainEventPublisher> logger)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));

        _logger.LogDebug("Enqueueing domain event {EventType} (ID: {EventId}) for tenant {TenantId}",
            typeof(TEvent).Name, domainEvent.EventId, domainEvent.TenantId);

        await _channel.Writer.WriteAsync(domainEvent, cancellationToken).ConfigureAwait(false);
    }
}
