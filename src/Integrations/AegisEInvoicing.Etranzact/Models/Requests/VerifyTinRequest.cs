using System.Text.Json.Serialization;

namespace AegisEInvoicing.Etranzact.Models.Requests;

/// <summary>
/// Request to verify a taxpayer TIN.
/// Equivalent to Interswitch's LookupWithTIN.
/// POST /api/v1/resource/verify-tin
/// </summary>
public sealed class VerifyTinRequest
{
    [JsonPropertyName("tin")]
    public string Tin { get; set; } = null!;
}
