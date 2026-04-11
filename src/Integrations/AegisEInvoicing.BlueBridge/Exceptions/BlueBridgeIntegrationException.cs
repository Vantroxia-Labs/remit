namespace AegisEInvoicing.BlueBridge.Exceptions;

/// <summary>
/// Exception thrown when BlueBridge API integration encounters an error.
/// </summary>
public sealed class BlueBridgeIntegrationException : Exception
{
    /// <summary>
    /// HTTP status code from the BlueBridge API response, if available.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Raw response body from the BlueBridge API, if available.
    /// </summary>
    public string? ResponseBody { get; }

    public BlueBridgeIntegrationException()
    {
    }

    public BlueBridgeIntegrationException(string message)
        : base(message)
    {
    }

    public BlueBridgeIntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BlueBridgeIntegrationException(string message, int statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
