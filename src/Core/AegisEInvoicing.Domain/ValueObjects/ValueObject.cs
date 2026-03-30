namespace AegisEInvoicing.Domain.ValueObjects;

/// <summary>
/// Base class for value objects
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components to use for equality comparison
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines equality based on value components
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Determines equality with another value object
    /// </summary>
    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Gets hash code based on equality components
    /// </summary>
    public override int GetHashCode()
    {
        var components = GetEqualityComponents()
            .Where(x => x != null)
            .Select(x => x!.GetHashCode())
            .ToArray();

        if (components.Length == 0)
            return 0;

        return components.Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);

    /// <summary>
    /// Creates a copy of this value object
    /// </summary>
    public ValueObject GetCopy() => (ValueObject)MemberwiseClone();
}