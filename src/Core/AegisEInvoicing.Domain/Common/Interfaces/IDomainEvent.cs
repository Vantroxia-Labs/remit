using MediatR;

namespace AegisEInvoicing.Domain.Common.Interfaces;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets the unique identifier of the event
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the version of the event schema
    /// </summary>
    int EventVersion { get; }
}