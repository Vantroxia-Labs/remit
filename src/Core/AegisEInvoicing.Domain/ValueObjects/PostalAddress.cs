using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.ValueObjects;

public class PostalAddress : ValueObject
{
    public string StreetName { get; }
    public string CityName { get; }
    public string PostalZone { get; }
    public string Country { get; }

    private PostalAddress()
    {
        StreetName = string.Empty;
        CityName = string.Empty;
        PostalZone = string.Empty;
        Country = string.Empty;
    }

    private PostalAddress(string streetName, string cityName, string postalZone, string country)
    {
        StreetName = streetName;
        CityName = cityName;
        PostalZone = postalZone;
        Country = country;
    }

    public static PostalAddress Create(string streetName, string cityName, string postalZone, string country)
    {
        if (string.IsNullOrWhiteSpace(streetName))
            throw new ArgumentException("Street name cannot be null or empty", nameof(streetName));

        if (string.IsNullOrWhiteSpace(cityName))
            throw new ArgumentException("City name cannot be null or empty", nameof(cityName));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be null or empty", nameof(country));

        return new PostalAddress(
            streetName.Trim(),
            cityName.Trim(),
            postalZone?.Trim() ?? string.Empty,
            country.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StreetName;
        yield return CityName;
        yield return PostalZone;
        yield return Country;
    }
}