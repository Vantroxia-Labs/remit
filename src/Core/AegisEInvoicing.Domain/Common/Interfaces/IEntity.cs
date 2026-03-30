namespace AegisEInvoicing.Domain.Common.Interfaces;

/// <summary>
/// Marker interface for domain entities
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the domain events associated with this entity
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from this entity
    /// </summary>
    void ClearDomainEvents();
}