using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string PostalCode { get; }
    public string? Lga { get; }

    // Parameterless constructor for Entity Framework
    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        Country = string.Empty;
        PostalCode = string.Empty;
        Lga = null;
    }

    private Address(string street, string city, string state, string country, string postalCode, string? lga = null)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        PostalCode = postalCode;
        Lga = lga;
    }

    public static Address Create(string street, string city, string state, string country, string postalCode, string? lga = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be null or empty", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be null or empty", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be null or empty", nameof(state));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be null or empty", nameof(country));

        return new Address(
            street.Trim(),
            city.Trim(),
            state.Trim(),
            country.Trim(),
            postalCode?.Trim() ?? string.Empty,
            lga?.Trim());
    }

    public string GetFormattedAddress()
    {
        var addressParts = new List<string> { Street, City, State };

        if (!string.IsNullOrWhiteSpace(PostalCode))
            addressParts.Add(PostalCode);

        addressParts.Add(Country);

        return string.Join(", ", addressParts);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return PostalCode;
        yield return Lga ?? string.Empty;
    }

    public override string ToString() => GetFormattedAddress();
}