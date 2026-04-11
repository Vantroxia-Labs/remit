using System.Text.Json.Serialization;

namespace AegisEInvoicing.Etranzact.Models.Requests;

/// <summary>
/// Request to update the payment status of a transmitted invoice.
/// PATCH /api/v1/app/invoice/update/{irn}
/// The IRN is passed as a URL path parameter, not in the body.
/// </summary>
public sealed class UpdatePaymentStatusRequest
{
    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = null!;
}
