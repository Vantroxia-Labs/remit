using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllCountries;

public sealed record GetAllCountriesResponse : GenericResponse
{
    public List<Country> Data { get; set; } = new();
}

public sealed record Country
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("alpha_2")]
    public string Alpha2 { get; set; } = null!;

    [JsonPropertyName("alpha_3")]
    public string Alpha3 { get; set; } = null!;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = null!;

    [JsonPropertyName("iso_3166_2")]
    public string Iso31662 { get; set; } = null!;

    [JsonPropertyName("region")]
    public string Region { get; set; } = null!;

    [JsonPropertyName("sub_region")]
    public string SubRegion { get; set; } = null!;

    [JsonPropertyName("intermediate_region")]
    public string IntermediateRegion { get; set; } = null!;

    [JsonPropertyName("region_code")]
    public string RegionCode { get; set; } = null!;

    [JsonPropertyName("sub_region_code")]
    public string SubRegionCode { get; set; } = null!;

    [JsonPropertyName("intermediate_region_code")]
    public string IntermediateRegionCode { get; set; } = null!;
}
