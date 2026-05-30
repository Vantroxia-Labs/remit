namespace AegisEInvoicing.Etranzact.Exceptions;

/// <summary>
/// Exception thrown when eTranzact API integration encounters an error.
/// </summary>
public sealed class EtranzactIntegrationException : Exception
{
    /// <summary>
    /// HTTP status code from the eTranzact API response.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Raw response body from the eTranzact API.
    /// </summary>
    public string? ResponseBody { get; }

    public EtranzactIntegrationException()
    {
    }

    public EtranzactIntegrationException(string message)
        : base(message)
    {
    }

    public EtranzactIntegrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public EtranzactIntegrationException(string message, int statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
