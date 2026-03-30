using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Requests.UpdateInvoice;

public sealed record UpdateInvoiceRequest
{
    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = "PENDING";

    [JsonPropertyName("invoice_reference")]
    public string? Reference { get; set; }
}