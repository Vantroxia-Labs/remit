using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models.Requests;

/// <summary>
/// Request body for Update Invoice endpoint.
/// PATCH /api/v1/invoices/update/:irn
/// </summary>
public sealed class UpdateInvoiceRequest
{
    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = null!;

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
}
