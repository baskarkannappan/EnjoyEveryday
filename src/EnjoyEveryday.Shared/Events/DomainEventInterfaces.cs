using System;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Shared.Events;

/// <summary>
/// Base contract for domain events representing meaningful occurrences within a tenant lifecycle.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    Guid TenantId { get; }
}

/// <summary>
/// Abstract record base to simplify domain event creation.
/// </summary>
public abstract record DomainEventBase(Guid TenantId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Handler contract for executing asynchronous domain event side-effects.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publisher contract for queuing or dispatching domain events.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
