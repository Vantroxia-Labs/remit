using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetVatExemptions;

public sealed record GetVatExemptionsResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public List<VatExemption> Data { get; set; } = [];
}

public sealed record VatExemption
{
    [JsonPropertyName("heading_no")]
    public string HeadingNo { get; set; } = null!;

    [JsonPropertyName("harmonized_system_code")]
    public string HarmonizedSystemCode { get; set; } = null!;

    [JsonPropertyName("tariff_category")]
    public string TariffCategory { get; set; } = null!;

    [JsonPropertyName("tariff")]
    public string Tariff { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;
}