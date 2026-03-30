using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.TransmitInvoice;

/// <summary>
/// Request to transmit a signed invoice to FIRS
/// </summary>
public sealed class TransmitInvoiceRequest
{
    /// <summary>
    /// Invoice Reference Number of the signed invoice
    /// </summary>
    [JsonPropertyName("IRN")]
    [Required]
    public string IRN { get; set; } = string.Empty;
}
