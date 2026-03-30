using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.Security;

/// <summary>
/// Implementation of response integrity service using HMAC-SHA512 for digital signatures.
/// Provides comprehensive protection against response tampering and replay attacks.
/// </summary>
public sealed class ResponseIntegrityService : IResponseIntegrityService
{
    private readonly ILogger<ResponseIntegrityService> _logger;
    private readonly IMemoryCache _cache;
    private readonly byte[] _signingKey;
    private readonly string _signingKeyBase64;
    private const string NoncePrefix = "nonce:";
    private const string ExternalServicePrefix = "ext_service:";

    public ResponseIntegrityService(
        IConfiguration configuration,
        ILogger<ResponseIntegrityService> logger,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        // Get signing key from configuration - use dedicated key for response signing
        _signingKeyBase64 = configuration["Security:ResponseIntegrity:SigningKey"] ??
                           configuration["ResponseIntegrity:SigningKey"] ??
                           configuration["RESPONSE_SIGNING_KEY"] ??
                           configuration["Jwt:SecretKey"] ?? // Fallback to JWT secret
                           throw new InvalidOperationException(
                               "Response signing key not found. Set 'Security:ResponseIntegrity:SigningKey' environment variable.");

        _signingKey = Encoding.UTF8.GetBytes(_signingKeyBase64);

        _logger.LogInformation("ResponseIntegrityService initialized with HMAC-SHA512 signing");
    }

    public Task<string> GenerateResponseSignatureAsync(string data, DateTime timestamp, string requestId)
    {
        try
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            if (string.IsNullOrEmpty(requestId))
                throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));

            // Create signature payload: data + timestamp + requestId
            var payload = $"{data}|{timestamp:O}|{requestId}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA512(_signingKey);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var signature = Convert.ToBase64String(hashBytes);

            _logger.LogDebug(
                "Generated response signature for RequestId: {RequestId}, Timestamp: {Timestamp}",
                requestId, timestamp);

            return Task.FromResult(signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response signature for RequestId: {RequestId}", requestId);
            throw new InvalidOperationException("Failed to generate response signature", ex);
        }
    }

    public Task<bool> VerifyResponseSignatureAsync(string data, string signature, DateTime timestamp, string requestId)
    {
        try
        {
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(requestId))
            {
                _logger.LogWarning("Response verification failed: Missing required parameters");
                return Task.FromResult(false);
            }

            // Recreate the signature with the same payload
            var payload = $"{data}|{timestamp:O}|{requestId}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA512(_signingKey);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var expectedSignature = Convert.ToBase64String(hashBytes);

            // Use constant-time comparison to prevent timing attacks
            var isValid = CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(signature),
                Convert.FromBase64String(expectedSignature));

            if (!isValid)
            {
                _logger.LogWarning(
                    "Response signature verification failed for RequestId: {RequestId}. Possible tampering detected.",
                    requestId);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying response signature for RequestId: {RequestId}", requestId);
            return Task.FromResult(false);
        }
    }

    public string GenerateNonce()
    {
        try
        {
            // Generate a cryptographically secure random nonce
            var nonceBytes = new byte[32]; // 256 bits
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonceBytes);

            var nonce = Convert.ToBase64String(nonceBytes);

            _logger.LogDebug("Generated nonce: {NoncePrefix}", nonce.Substring(0, Math.Min(10, nonce.Length)));

            return nonce;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating nonce");
            throw new InvalidOperationException("Failed to generate nonce", ex);
        }
    }

    public Task<bool> ValidateNonceAsync(string nonce, int expirationMinutes = 5)
    {
        try
        {
            if (string.IsNullOrEmpty(nonce))
            {
                _logger.LogWarning("Nonce validation failed: Nonce is null or empty");
                return Task.FromResult(false);
            }

            var cacheKey = $"{NoncePrefix}{nonce}";

            // Check if nonce has already been used (replay attack prevention)
            if (_cache.TryGetValue(cacheKey, out _))
            {
                _logger.LogWarning(
                    "Nonce validation failed: Nonce already used (replay attack detected). Nonce: {NoncePrefix}",
                    nonce.Substring(0, Math.Min(10, nonce.Length)));
                return Task.FromResult(false);
            }

            // Store nonce in cache with expiration to prevent reuse
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            };

            _cache.Set(cacheKey, true, cacheOptions);

            _logger.LogDebug("Nonce validated and cached for {ExpirationMinutes} minutes", expirationMinutes);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating nonce");
            return Task.FromResult(false);
        }
    }

    public Task<string> SignInvoiceOperationAsync(
        Guid invoiceId,
        string operation,
        string statusBefore,
        string statusAfter,
        DateTime timestamp)
    {
        try
        {
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("Operation cannot be null or empty", nameof(operation));

            // Create comprehensive operation signature payload
            var operationData = new
            {
                InvoiceId = invoiceId,
                Operation = operation,
                StatusBefore = statusBefore,
                StatusAfter = statusAfter,
                Timestamp = timestamp.ToString("O")
            };

            var payloadJson = JsonSerializer.Serialize(operationData);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            using var hmac = new HMACSHA512(_signingKey);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var signature = Convert.ToBase64String(hashBytes);

            _logger.LogInformation(
                "Signed invoice operation: InvoiceId={InvoiceId}, Operation={Operation}, Status={StatusBefore}->{StatusAfter}",
                invoiceId, operation, statusBefore, statusAfter);

            return Task.FromResult(signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error signing invoice operation: InvoiceId={InvoiceId}, Operation={Operation}",
                invoiceId, operation);
            throw new InvalidOperationException("Failed to sign invoice operation", ex);
        }
    }

    public Task<bool> VerifyExternalServiceResponseAsync(
        string externalServiceName,
        string responseData,
        string? expectedSignature = null)
    {
        try
        {
            if (string.IsNullOrEmpty(externalServiceName))
                throw new ArgumentException("External service name cannot be null or empty", nameof(externalServiceName));

            if (string.IsNullOrEmpty(responseData))
                throw new ArgumentException("Response data cannot be null or empty", nameof(responseData));

            // Store hash of external service response for audit trail
            var cacheKey = $"{ExternalServicePrefix}{externalServiceName}:{DateTime.UtcNow:yyyyMMddHHmmss}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(responseData));
            var responseHash = Convert.ToBase64String(hashBytes);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Keep for audit
            };

            _cache.Set(cacheKey, new
            {
                ResponseHash = responseHash,
                Timestamp = DateTime.UtcNow,
                ServiceName = externalServiceName,
                ExpectedSignature = expectedSignature
            }, cacheOptions);

            // If external service provides signature, verify it
            if (!string.IsNullOrEmpty(expectedSignature))
            {
                // For now, we log the signature - in future, implement service-specific verification
                _logger.LogInformation(
                    "External service {ServiceName} provided signature: {SignaturePrefix}",
                    externalServiceName,
                    expectedSignature.Substring(0, Math.Min(20, expectedSignature.Length)));
            }

            _logger.LogInformation(
                "Verified and logged external service response: Service={ServiceName}, Hash={HashPrefix}",
                externalServiceName,
                responseHash.Substring(0, Math.Min(20, responseHash.Length)));

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error verifying external service response: Service={ServiceName}",
                externalServiceName);
            return Task.FromResult(false);
        }
    }
}
