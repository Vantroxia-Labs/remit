using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetInvoiceType;

public sealed record GetInvoiceTypeResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public List<InvoiceType> Data { get; set; } = [];
}

public sealed record InvoiceType
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}