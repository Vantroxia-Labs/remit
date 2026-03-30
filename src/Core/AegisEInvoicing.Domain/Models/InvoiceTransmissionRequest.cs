using AegisEInvoicing.Domain.Enums;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Domain.Models;

/// <summary>
/// Webhook payload for invoice transmission notifications
/// </summary>
public class InvoiceTransmissionRequest
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("message")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InvoiceStatus? Message { get; set; }
}