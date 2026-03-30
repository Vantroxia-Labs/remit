using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetTaxCategories;

public sealed record GetTaxCategoriesResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public List<TaxCategory> Data { get; set; } = new();
}

public sealed record TaxCategory
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;

    [JsonPropertyName("percent")]
    public string Percent { get; set; } = null!;
}