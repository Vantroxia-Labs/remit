using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.Token;

/// <summary>
/// Request model for Interswitch authentication token
/// </summary>
public sealed class TokenRequest
{
    /// <summary>
    /// Client ID provided by Interswitch
    /// </summary>
    [JsonPropertyName("ClientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret provided by Interswitch
    /// </summary>
    [JsonPropertyName("ClientSecret")]
    public string ClientSecret { get; set; } = string.Empty;
}
