using System.Text.Json.Serialization;

namespace AegisEInvoicing.Etranzact.Models.Requests;

/// <summary>
/// Request to validate an Invoice Reference Number (IRN).
/// Equivalent to Interswitch's LookupWithIRN.
/// POST /api/v1/app/invoice/validate-irn
/// </summary>
public sealed class ValidateIrnRequest
{
    [JsonPropertyName("irn")]
    public string Irn { get; set; } = null!;

    [JsonPropertyName("business_id")]
    public string BusinessId { get; set; } = null!;
}
