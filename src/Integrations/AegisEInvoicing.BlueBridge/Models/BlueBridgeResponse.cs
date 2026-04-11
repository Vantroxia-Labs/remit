using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models;

/// <summary>
/// Base response wrapper for BlueBridge API responses that carry a data or payload field.
/// The <c>status</c> field is sometimes an integer (200) and sometimes a string ("200 OK"),
/// so it is captured as a <see cref="JsonElement"/> and normalised via <see cref="IsSuccess"/>.
/// </summary>
/// <typeparam name="T">Type of the data or payload.</typeparam>
public class BlueBridgeResponse<T>
{
    [JsonPropertyName("status")]
    public JsonElement RawStatus { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Used by endpoints that wrap their response under a <c>data</c> key.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Used by endpoints that wrap their response under a <c>payload</c> key.
    /// </summary>
    [JsonPropertyName("payload")]
    public T? Payload { get; set; }

    [JsonIgnore]
    public bool IsSuccess => RawStatus.ValueKind switch
    {
        JsonValueKind.Number => RawStatus.TryGetInt32(out var n) && n >= 200 && n < 300,
        JsonValueKind.String => RawStatus.GetString() is string s
                                && (s.StartsWith("2", StringComparison.Ordinal)
                                    || s.Equals("ok", StringComparison.OrdinalIgnoreCase)),
        _ => false
    };
}

/// <summary>
/// Response wrapper for endpoints that return only a status and message (no data body).
/// </summary>
public class BlueBridgeResponse : BlueBridgeResponse<object?>
{
}
