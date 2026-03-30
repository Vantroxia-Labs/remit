namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for ensuring response integrity through HMAC-based digital signatures.
/// Protects against response tampering by signing all critical API responses.
/// </summary>
public interface IResponseIntegrityService
{
    /// <summary>
    /// Generates HMAC signature for response data to prevent tampering
    /// </summary>
    /// <param name="data">The response data to sign (JSON string)</param>
    /// <param name="timestamp">Timestamp of response generation</param>
    /// <param name="requestId">Unique request identifier for correlation</param>
    /// <returns>Base64-encoded HMAC-SHA512 signature</returns>
    Task<string> GenerateResponseSignatureAsync(string data, DateTime timestamp, string requestId);

    /// <summary>
    /// Verifies the integrity of a signed response
    /// </summary>
    /// <param name="data">The response data (JSON string)</param>
    /// <param name="signature">The signature to verify</param>
    /// <param name="timestamp">Timestamp of response generation</param>
    /// <param name="requestId">Unique request identifier</param>
    /// <returns>True if signature is valid and response has not been tampered with</returns>
    Task<bool> VerifyResponseSignatureAsync(string data, string signature, DateTime timestamp, string requestId);

    /// <summary>
    /// Generates a unique nonce for request replay protection
    /// </summary>
    /// <returns>Unique nonce value</returns>
    string GenerateNonce();

    /// <summary>
    /// Validates a nonce to prevent request replay attacks
    /// </summary>
    /// <param name="nonce">The nonce to validate</param>
    /// <param name="expirationMinutes">How long the nonce is valid (default: 5 minutes)</param>
    /// <returns>True if nonce is valid and has not been used before</returns>
    Task<bool> ValidateNonceAsync(string nonce, int expirationMinutes = 5);

    /// <summary>
    /// Signs critical invoice operation responses with additional metadata
    /// </summary>
    /// <param name="invoiceId">Invoice identifier</param>
    /// <param name="operation">Operation performed (Validate, Sign, Transmit, etc.)</param>
    /// <param name="statusBefore">Status before operation</param>
    /// <param name="statusAfter">Status after operation</param>
    /// <param name="timestamp">Operation timestamp</param>
    /// <returns>Tamper-proof signature of the operation</returns>
    Task<string> SignInvoiceOperationAsync(
        Guid invoiceId,
        string operation,
        string statusBefore,
        string statusAfter,
        DateTime timestamp);

    /// <summary>
    /// Verifies external service response signature (FIRS, Interswitch)
    /// </summary>
    /// <param name="externalServiceName">Name of the external service</param>
    /// <param name="responseData">Response data from external service</param>
    /// <param name="expectedSignature">Expected signature if provided by service</param>
    /// <returns>True if response can be trusted</returns>
    Task<bool> VerifyExternalServiceResponseAsync(
        string externalServiceName,
        string responseData,
        string? expectedSignature = null);
}
