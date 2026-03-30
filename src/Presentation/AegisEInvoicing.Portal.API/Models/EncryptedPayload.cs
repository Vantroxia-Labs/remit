using System.Text.Json.Serialization;

namespace AegisEInvoicing.Portal.API.Models;

/// <summary>
/// Model for encrypted request payloads
/// Used by PayloadDecryptionMiddleware to decrypt sensitive request data
/// </summary>
public class EncryptedPayload
{
    /// <summary>
    /// Base64-encoded AES-encrypted JSON payload
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded 16-byte initialization vector (IV)
    /// </summary>
    [JsonPropertyName("iv")]
    public string Iv { get; set; } = string.Empty;
}
