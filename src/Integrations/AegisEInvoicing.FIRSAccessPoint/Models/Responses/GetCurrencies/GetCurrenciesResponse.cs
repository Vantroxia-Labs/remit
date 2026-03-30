using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetCurrencies;

public sealed record GetCurrenciesResponse : GenericResponse
{
    public List<Currency> Data { get; set; } = new();
}

public sealed record Currency
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("symbol_native")]
    public string SymbolNative { get; set; } = null!;

    [JsonPropertyName("decimal_digits")]
    public int DecimalDigits { get; set; }

    [JsonPropertyName("rounding")]
    public double Rounding { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("name_plural")]
    public string NamePlural { get; set; } = null!;
}