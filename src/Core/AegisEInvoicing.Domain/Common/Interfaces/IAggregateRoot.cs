namespace AegisEInvoicing.Domain.Common.Interfaces;

/// <summary>
/// Marker interface for aggregate roots
/// </summary>
public interface IAggregateRoot : IEntity
{
    /// <summary>
    /// Gets the version for optimistic concurrency control
    /// </summary>
    int Version { get; }
}