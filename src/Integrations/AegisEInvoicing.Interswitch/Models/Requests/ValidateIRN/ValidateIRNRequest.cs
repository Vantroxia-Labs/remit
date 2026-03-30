using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.ValidateIRN;

/// <summary>
/// Request to validate an Invoice Reference Number (IRN)
/// </summary>
public sealed class ValidateIRNRequest
{
    /// <summary>
    /// Internal invoice reference from the business system
    /// </summary>
    [JsonPropertyName("invoice_reference")]
    [Required]
    public string InvoiceReference { get; set; } = string.Empty;

    /// <summary>
    /// Business identifier in the Interswitch system
    /// </summary>
    [JsonPropertyName("business_id")]
    [Required]
    public string BusinessId { get; set; } = string.Empty;

    /// <summary>
    /// Invoice Reference Number to validate (format: NISW008608-6AFCD0BD-20250930)
    /// </summary>
    [JsonPropertyName("irn")]
    [Required]
    public string IRN { get; set; } = string.Empty;
}
