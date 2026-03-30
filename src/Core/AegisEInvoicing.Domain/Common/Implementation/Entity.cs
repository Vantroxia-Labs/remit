using AegisEInvoicing.Domain.Common.Interfaces;

namespace AegisEInvoicing.Domain.Common.Implementation;

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class Entity : IEntity, IEquatable<Entity>
{
    private int? _requestedHashCode;

    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets the domain events for this entity
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => [];

    /// <summary>
    /// Clears domain events (implemented in AggregateRoot)
    /// </summary>
    public virtual void ClearDomainEvents() { }

    /// <summary>
    /// Determines if this entity is transient (not persisted)
    /// </summary>
    public bool IsTransient() => Id == Guid.Empty;

    /// <summary>
    /// Determines equality based on identity
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
            return false;

        if (ReferenceEquals(this, entity))
            return true;

        if (GetType() != entity.GetType())
            return false;

        if (entity.IsTransient() || IsTransient())
            return false;

        return entity.Id == Id;
    }

    /// <summary>
    /// Determines equality with another entity
    /// </summary>
    public bool Equals(Entity? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return !IsTransient() && !other.IsTransient() && Id == other.Id;
    }

    /// <summary>
    /// Gets the hash code based on the entity's identity
    /// </summary>
    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            _requestedHashCode ??= HashCode.Combine(Id, 31);
            return _requestedHashCode.Value;
        }

        return base.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}