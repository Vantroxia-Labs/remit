using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models;

public record GenericResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("error")]
    public Error? Error { get; set; }
}

public class Error
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("handler")]
    public string? Handler { get; set; }
    [JsonPropertyName("details")]
    public string? Details { get; set; }
    [JsonPropertyName("public_message")]
    public string? PublicMessage { get; set; }
}