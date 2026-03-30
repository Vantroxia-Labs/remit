namespace AegisEInvoicing.Domain.ValueObjects;

public class Currency : ValueObject
{
   
    public string Name { get; }
    public string Code { get; }

    private Currency()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    private Currency(string name, string code)
    {
        Name = name;
        Code = code;
    }

    public static Currency Create(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be null or empty", nameof(code));

        if (code.Length < 3)
            throw new ArgumentException("Currency code must be at least 3 characters long", nameof(code));

        if (!code.All(char.IsLetter))
            throw new ArgumentException("Currency code must contain only alphabetic characters", nameof(code));

        return new Currency(name, code);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Code;
    }
}
