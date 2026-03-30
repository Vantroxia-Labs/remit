using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateIRN;

public sealed record ValidateIrnRequest
{
    [JsonPropertyName("invoice_reference")]
    public string InvoiceReference { get; set; } = null!;

    [JsonPropertyName("business_id")]
    public string BusinessId { get; set; } = null!;

    [JsonPropertyName("irn")]
    public string Irn { get; set; } = null!;
}