using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.LookupWithIRN;

/// <summary>
/// Request to lookup business information using IRN
/// </summary>
public sealed class LookupWithIRNRequest
{
    /// <summary>
    /// Invoice Reference Number
    /// </summary>
    [JsonPropertyName("IRN")]
    [Required]
    public string IRN { get; set; } = string.Empty;
}
