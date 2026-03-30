using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.LookupWithTIN;

/// <summary>
/// Request to lookup business information using TIN
/// </summary>
public sealed class LookupWithTINRequest
{
    /// <summary>
    /// Tax Identification Number (format: 15631438-0242)
    /// </summary>
    [JsonPropertyName("TIN")]
    [Required]
    public string TIN { get; set; } = string.Empty;
}
