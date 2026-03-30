using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Responses.DownloadInvoice;

/// <summary>
/// Response from DownloadInvoice endpoint containing encrypted invoice data
/// </summary>
public sealed class DownloadInvoiceResponse : InterswitchResponse<DownloadInvoiceData>
{
}

public sealed class DownloadInvoiceData
{
    /// <summary>
    /// Initialization vector in hexadecimal format
    /// </summary>
    [JsonPropertyName("iv_hex")]
    public string IvHex { get; set; } = string.Empty;

    /// <summary>
    /// Public key for decryption
    /// </summary>
    [JsonPropertyName("pub")]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded encrypted invoice data
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}
