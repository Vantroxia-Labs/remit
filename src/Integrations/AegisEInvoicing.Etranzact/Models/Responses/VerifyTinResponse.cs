using System.Text.Json.Serialization;
using AegisEInvoicing.Etranzact.Models;

namespace AegisEInvoicing.Etranzact.Models.Responses;

/// <summary>
/// Response from the verify TIN endpoint.
/// Equivalent to Interswitch's LookupWithTIN response.
/// </summary>
public sealed class VerifyTinResponse : EtranzactResponse<VerifyTinData>
{
}

public sealed class VerifyTinData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("tin")]
    public string? Tin { get; set; }

    [JsonPropertyName("taxpayer_name")]
    public string? TaxpayerName { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("taxofficer_id")]
    public string? TaxOfficerId { get; set; }

    [JsonPropertyName("taxofficer_name")]
    public string? TaxOfficerName { get; set; }

    [JsonPropertyName("rc_number")]
    public string? RcNumber { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}
