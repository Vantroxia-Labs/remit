using AegisEInvoicing.Domain.Common.Interfaces;

namespace AegisEInvoicing.Domain.Common.Implementation;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the domain event
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.CreateVersion7();
        OccurredOn = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public Guid EventId { get; init; }

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; }

    /// <inheritdoc/>
    public virtual int EventVersion => 1;
}