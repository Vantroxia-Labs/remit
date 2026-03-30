using AegisEInvoicing.Domain.Common.Interfaces;

namespace AegisEInvoicing.Domain.Common.Implementation;

/// <summary>
/// Base class for aggregate roots in the domain
/// </summary>
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the collection of domain events
    /// </summary>
    public new IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to this aggregate
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        Guard.Against.Null(domainEvent, nameof(domainEvent));
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from this aggregate
    /// </summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from this aggregate
    /// </summary>
    public override void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Gets the version of this aggregate for optimistic concurrency
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Increments the version for optimistic concurrency control
    /// </summary>
    protected void IncrementVersion()
    {
        Version++;
    }
}