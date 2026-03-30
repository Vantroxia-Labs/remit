using System.Text.Json.Serialization;

namespace AegisEInvoicing.Portal.API.Models;

/// <summary>
/// Standard API response wrapper with tamper protection
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public object? Details { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public string? TraceId { get; set; }
    public string? DeveloperMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// HMAC-SHA512 signature for response integrity verification.
    /// Protects against response tampering attacks.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Request ID for correlation and replay attack prevention
    /// </summary>
    public string? RequestId { get; set; }
}
