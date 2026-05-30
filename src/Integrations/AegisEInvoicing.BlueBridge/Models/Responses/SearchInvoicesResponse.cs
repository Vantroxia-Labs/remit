using System.Text.Json.Serialization;
using AegisEInvoicing.BlueBridge.Models;

namespace AegisEInvoicing.BlueBridge.Models.Responses;

/// <summary>
/// Response from Search Invoices endpoint.
/// GET /api/v1/invoices/:businessId
/// </summary>
public sealed class SearchInvoicesResponse : BlueBridgeResponse<List<SearchInvoiceRecord>>
{
}

public sealed class SearchInvoiceRecord
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("invoice_type_code")]
    public string? InvoiceTypeCode { get; set; }

    [JsonPropertyName("payment_status")]
    public string? PaymentStatus { get; set; }
}
