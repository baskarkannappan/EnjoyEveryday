using System;
using System.Threading.Channels;
using EnjoyEveryday.Shared.Events;

namespace EnjoyEveryday.Infrastructure.Events;

/// <summary>
/// Singleton in-memory event channel backed by System.Threading.Channels.
/// Provides bounded or unbounded lock-free queuing without any third-party message brokers.
/// </summary>
public class InProcessDomainEventChannel
{
    private readonly Channel<IDomainEvent> _channel;

    public InProcessDomainEventChannel()
    {
        // Unbounded channel with single/multi reader/writer support
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateUnbounded<IDomainEvent>(options);
    }

    public ChannelWriter<IDomainEvent> Writer => _channel.Writer;
    public ChannelReader<IDomainEvent> Reader => _channel.Reader;
}
