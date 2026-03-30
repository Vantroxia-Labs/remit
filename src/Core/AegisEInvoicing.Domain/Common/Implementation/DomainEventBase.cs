using AegisEInvoicing.Domain.Common.Interfaces;

namespace AegisEInvoicing.Domain.Common.Implementation;

/// <summary>
/// Base implementation for domain events with required properties
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public int EventVersion { get; } = 1;
}