using AegisEInvoicing.Domain.Common.Interfaces;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Event bus for publishing domain events
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent;

    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent;
}
