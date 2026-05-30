using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models.Responses;

/// <summary>
/// Response from Generate IRN endpoint.
/// GET /api/v1/invoices/generate-irn?reference={reference}
/// </summary>
public sealed class GenerateIrnResponse
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSuccess => !string.IsNullOrWhiteSpace(Irn);
}
