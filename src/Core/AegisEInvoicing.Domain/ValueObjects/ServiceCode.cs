namespace AegisEInvoicing.Domain.ValueObjects;

public sealed class ServiceCode : ValueObject
{
    public string Code { get; }
    public string Name { get; }

    private ServiceCode(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Service code cannot be empty.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Service name cannot be empty.", nameof(name));

        Code = code;
        Name = name;
    }

    /// <summary>
    /// Factory method to create a new ServiceCode value object.
    /// </summary>
    public static ServiceCode Create(string code, string name) => new(code, name);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
        yield return Name;
    }

    public override string ToString() => $"{Code} - {Name}";
}