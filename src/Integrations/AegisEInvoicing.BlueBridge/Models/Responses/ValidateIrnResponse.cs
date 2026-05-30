using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models.Responses;

/// <summary>
/// Response from Validate IRN endpoint.
/// POST /api/v1/invoices/validate-irn
/// </summary>
public sealed class ValidateIrnResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSuccess => Valid;
}
