using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Responses.Token;

/// <summary>
/// Response model for Interswitch authentication token
/// </summary>
public sealed class TokenResponse
{
    /// <summary>
    /// The access token (JWT)
    /// </summary>
    [JsonPropertyName("token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (typically "bearer")
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
