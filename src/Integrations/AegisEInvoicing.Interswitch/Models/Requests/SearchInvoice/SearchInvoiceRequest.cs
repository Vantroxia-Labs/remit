using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.SearchInvoice;

/// <summary>
/// Request to search for an invoice by IRN
/// </summary>
public sealed class SearchInvoiceRequest
{
    /// <summary>
    /// Invoice Reference Number to search for
    /// </summary>
    [JsonPropertyName("IRN")]
    [Required]
    public string IRN { get; set; } = string.Empty;
}
