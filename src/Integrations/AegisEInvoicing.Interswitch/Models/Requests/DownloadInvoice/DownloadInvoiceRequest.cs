using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.DownloadInvoice;

/// <summary>
/// Request to download encrypted invoice
/// </summary>
public sealed class DownloadInvoiceRequest
{
    /// <summary>
    /// Invoice Reference Number
    /// </summary>
    [JsonPropertyName("IRN")]
    [Required]
    public string IRN { get; set; } = string.Empty;
}
