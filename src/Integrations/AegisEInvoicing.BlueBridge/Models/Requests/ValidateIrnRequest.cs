using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models.Requests;

/// <summary>
/// Request body for Validate IRN endpoint.
/// POST /api/v1/invoices/validate-irn
/// </summary>
public sealed class ValidateIrnRequest
{
    [JsonPropertyName("irn")]
    public string Irn { get; set; } = null!;

    [JsonPropertyName("invoice_reference")]
    public string InvoiceReference { get; set; } = null!;

    [JsonPropertyName("business_id")]
    public string BusinessId { get; set; } = null!;
}
