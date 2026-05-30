using System.Text.Json.Serialization;

namespace AegisEInvoicing.Etranzact.Models;

/// <summary>
/// Base response wrapper for all eTranzact API responses.
/// Shape: { "status": "200 OK", "message": "...", "data": {...}, "execTime": "768", "error": "" }
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class EtranzactResponse<T>
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("execTime")]
    public string? ExecTime { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonIgnore]
    public int StatusCode => int.TryParse(Status?.Split(' ')[0], out var code) ? code : 0;

    [JsonIgnore]
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300 && string.IsNullOrWhiteSpace(Error);

    [JsonIgnore]
    public bool IsError => !IsSuccess;
}

/// <summary>
/// Response wrapper for endpoints that return no data payload.
/// </summary>
public sealed class EtranzactResponse : EtranzactResponse<object?>
{
}
