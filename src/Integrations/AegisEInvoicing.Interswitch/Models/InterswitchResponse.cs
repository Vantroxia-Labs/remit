using System.Text.Json.Serialization;
using AegisEInvoicing.Interswitch.Converters;

namespace AegisEInvoicing.Interswitch.Models;

/// <summary>
/// Base response wrapper for all Interswitch API responses
/// Handles both success and error response structures
/// </summary>
/// <typeparam name="T">The type of data in the response</typeparam>
public class InterswitchResponse<T>
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Response data payload (can be T for success or InterswitchErrorData for errors)
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Success indicator from outer wrapper
    /// </summary>
    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    /// <summary>
    /// Error details (present when data is null and an error occurred)
    /// </summary>
    [JsonPropertyName("error")]
    public InterswitchErrorDetails? ErrorDetails { get; set; }

    /// <summary>
    /// Indicates if the request was successful
    /// Checks the Success flag, HTTP status code, and absence of error data
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => (Success ?? true) && Code >= 200 && Code < 300 && ErrorDetails == null && !(Data is InterswitchErrorData);

    /// <summary>
    /// Indicates if this is an error response
    /// </summary>
    [JsonIgnore]
    public bool IsError => !IsSuccess;

    /// <summary>
    /// Gets the error details from either the error field or nested in Data
    /// </summary>
    [JsonIgnore]
    public InterswitchErrorDetails? Error
    {
        get
        {
            // First check the direct error field
            if (ErrorDetails != null)
            {
                return ErrorDetails;
            }

            // Then check if Data contains InterswitchErrorData
            if (Data is InterswitchErrorData errorData)
            {
                return errorData.Error;
            }

            return null;
        }
    }
}

/// <summary>
/// Wrapper for responses that come with a success flag
/// Example: { "success": true, "data": { ... } }
/// </summary>
/// <typeparam name="T">The type of data in the inner data object</typeparam>
public class InterswitchWrappedResponse<T> where T : class
{
    /// <summary>
    /// Indicates if the outer request was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The actual response data (which contains code, data, message, error)
    /// Supports both stringified JSON and direct object deserialization
    /// </summary>
    [JsonPropertyName("data")]
    public InterswitchResponse<T>? Data { get; set; }

    /// <summary>
    /// Indicates if the entire request was successful
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Success && Data != null && Data.IsSuccess;

    /// <summary>
    /// Gets the error information if available
    /// </summary>
    [JsonIgnore]
    public InterswitchErrorDetails? Error
    {
        get
        {
            if (Data != null && Data.Data is InterswitchErrorData errorData)
            {
                return errorData.Error;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets the error message
    /// </summary>
    [JsonIgnore]
    public string? ErrorMessage => Data?.Message ?? Error?.PublicMessage;
}

/// <summary>
/// Error data structure returned in the data field when an error occurs
/// </summary>
public class InterswitchErrorData
{
    /// <summary>
    /// HTTP status code (duplicated in error responses)
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Data field (usually null for errors)
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Detailed error information
    /// </summary>
    [JsonPropertyName("error")]
    public InterswitchErrorDetails? Error { get; set; }
}

/// <summary>
/// Detailed error information
/// </summary>
public class InterswitchErrorDetails
{
    /// <summary>
    /// Unique error identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Handler that generated the error
    /// </summary>
    [JsonPropertyName("handler")]
    public string? Handler { get; set; }

    /// <summary>
    /// Technical error details
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>
    /// User-friendly error message
    /// </summary>
    [JsonPropertyName("public_message")]
    public string? PublicMessage { get; set; }
}

/// <summary>
/// Simple OK response structure for successful operations
/// Example: { "code": 201, "data": { "ok": true } }
/// </summary>
public sealed class OkResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
}
