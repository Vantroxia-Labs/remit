namespace AegisEInvoicing.Interswitch.Exceptions;

/// <summary>
/// Exception thrown when Interswitch API integration encounters an error
/// </summary>
public sealed class InterswitchIntegrationException : Exception
{
    /// <summary>
    /// HTTP status code from the Interswitch API response
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Raw response body from the Interswitch API
    /// </summary>
    public string? ResponseBody { get; }

    public InterswitchIntegrationException()
    {
    }

    public InterswitchIntegrationException(string message)
        : base(message)
    {
    }

    public InterswitchIntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InterswitchIntegrationException(string message, int statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
