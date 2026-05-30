using System.Text.Json.Serialization;

namespace AegisEInvoicing.Etranzact.Models.Requests;

/// <summary>
/// Request to transmit a signed invoice to NRS for official registration.
/// POST /api/v1/app/invoice/transmit
/// </summary>
public sealed class TransmitInvoiceRequest
{
    [JsonPropertyName("irn")]
    public string Irn { get; set; } = null!;
}
