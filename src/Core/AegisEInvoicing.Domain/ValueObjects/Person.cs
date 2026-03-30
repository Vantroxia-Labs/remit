namespace AegisEInvoicing.Domain.ValueObjects;

public class Person : ValueObject
{
    public string? FirstName { get; }
    public string? FamilyName { get; }

    private Person()
    {
    }

    private Person(string? firstName, string? familyName)
    {
        FirstName = firstName;
        FamilyName = familyName;
    }

    public static Person Create(string? firstName = null, string? familyName = null)
    {
        return new Person(firstName?.Trim(), familyName?.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName ?? string.Empty;
        yield return FamilyName ?? string.Empty;
    }
}