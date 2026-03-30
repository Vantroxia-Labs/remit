using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.Authentication;

public sealed record AuthenticationResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public AuthenticationBodyResponse Data { get; set; } = null!;
}

public sealed record AuthenticationBodyResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("received_at")]
    public string ReceivedAt { get; set; } = null!;

    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }
}