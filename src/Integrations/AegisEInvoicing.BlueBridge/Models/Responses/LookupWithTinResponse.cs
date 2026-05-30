using System.Text.Json.Serialization;
using AegisEInvoicing.BlueBridge.Models;

namespace AegisEInvoicing.BlueBridge.Models.Responses;

/// <summary>
/// Response from Lookup Invoices By TIN endpoint.
/// GET /api/v1/invoices/transmit/lookup/tin/:tin
/// </summary>
public sealed class LookupWithTinResponse : BlueBridgeResponse<List<TinInvoiceRecord>>
{
}

public sealed class TinInvoiceRecord
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("tin")]
    public string? Tin { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
