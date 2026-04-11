using System.Text.Json.Serialization;
using AegisEInvoicing.BlueBridge.Models;

namespace AegisEInvoicing.BlueBridge.Models.Responses;

/// <summary>
/// Response from Lookup Invoice By IRN endpoint.
/// GET /api/v1/invoices/lookup/:irn
/// </summary>
public sealed class LookupWithIrnResponse : BlueBridgeResponse<IrnInvoiceRecord>
{
}

public sealed class IrnInvoiceRecord
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("transmitted_at")]
    public string? TransmittedAt { get; set; }
}
