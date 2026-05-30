using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models.Responses;

/// <summary>
/// Response from Invoice Service Health Check endpoint.
/// GET /api/v1/invoices/health
/// </summary>
public sealed class HealthCheckResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsHealthy => Status?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;
}
