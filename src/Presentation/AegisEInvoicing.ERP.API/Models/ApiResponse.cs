namespace AegisEInvoicing.ERP.API.Models;

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? TraceId { get; set; }
    public string? DeveloperMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
