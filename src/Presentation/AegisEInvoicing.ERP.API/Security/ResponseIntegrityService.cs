using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace AegisEInvoicing.ERP.API.Security;

/// <summary>
/// Service for generating and validating response integrity signatures.
/// Addresses VAPT finding: Response tampering vulnerability.
/// Uses HMAC-SHA256 to sign responses, preventing man-in-the-middle modifications.
/// </summary>
public interface IResponseIntegrityService
{
    /// <summary>
    /// Generates an HMAC-SHA256 signature for the response body
    /// </summary>
    /// <param name="responseBody">The response body to sign</param>
    /// <param name="timestamp">The UTC timestamp of the response</param>
    /// <param name="nonce">A unique nonce for anti-replay protection</param>
    /// <param name="requestId">Optional request ID for correlation</param>
    /// <returns>Base64-encoded HMAC signature</returns>
    string GenerateSignature(string responseBody, DateTime timestamp, string nonce, string? requestId = null);

    /// <summary>
    /// Validates a response signature
    /// </summary>
    /// <param name="responseBody">The response body that was signed</param>
    /// <param name="signature">The signature to validate</param>
    /// <param name="timestamp">The timestamp used in signing</param>
    /// <param name="nonce">The nonce used in signing</param>
    /// <param name="requestId">Optional request ID used in signing</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    bool ValidateSignature(string responseBody, string signature, DateTime timestamp, string nonce, string? requestId = null);

    /// <summary>
    /// Generates a cryptographically secure nonce
    /// </summary>
    /// <returns>A unique nonce string</returns>
    string GenerateNonce();

    /// <summary>
    /// Checks if a nonce has been used before (anti-replay)
    /// </summary>
    /// <param name="nonce">The nonce to check</param>
    /// <returns>True if nonce is fresh (not used before), false if it's a replay</returns>
    bool IsNonceFresh(string nonce);

    /// <summary>
    /// Records a nonce as used to prevent replay attacks
    /// </summary>
    /// <param name="nonce">The nonce to record</param>
    void RecordNonceUsage(string nonce);
}

/// <summary>
/// Implementation of response integrity service using HMAC-SHA256.
/// </summary>
public class ResponseIntegrityService : IResponseIntegrityService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResponseIntegrityService> _logger;
    private readonly IMemoryCache _nonceCache;
    private readonly byte[] _signingKey;

    // Configuration constants
    private const int NonceExpirationMinutes = 5; // Nonces expire after 5 minutes
    private const int DefaultKeyLengthBytes = 32; // 256 bits for HMAC-SHA256

    public ResponseIntegrityService(
        IConfiguration configuration,
        ILogger<ResponseIntegrityService> logger,
        IMemoryCache nonceCache)
    {
        _configuration = configuration;
        _logger = logger;
        _nonceCache = nonceCache;

        // Get or generate signing key
        _signingKey = GetOrGenerateSigningKey();
    }

    public string GenerateSignature(string responseBody, DateTime timestamp, string nonce, string? requestId = null)
    {
        try
        {
            // Create the message to sign: timestamp|nonce|requestId|body
            // This format ensures all components are included in the signature
            var messageToSign = BuildSigningMessage(responseBody, timestamp, nonce, requestId);

            using var hmac = new HMACSHA256(_signingKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(messageToSign));

            var signature = Convert.ToBase64String(hash);

            _logger.LogDebug(
                "Generated response signature for request {RequestId} with nonce {Nonce}",
                requestId ?? "N/A",
                nonce);

            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate response signature");
            throw new InvalidOperationException("Failed to generate response signature", ex);
        }
    }

    public bool ValidateSignature(string responseBody, string signature, DateTime timestamp, string nonce, string? requestId = null)
    {
        try
        {
            // Check timestamp is within acceptable window (prevents replay of old responses)
            var timestampAge = DateTime.UtcNow - timestamp;
            var maxAgeMinutes = _configuration.GetValue("Security:ResponseIntegrity:MaxTimestampAgeMinutes", 5);

            if (Math.Abs(timestampAge.TotalMinutes) > maxAgeMinutes)
            {
                _logger.LogWarning(
                    "Response signature validation failed: Timestamp too old ({Age} minutes)",
                    timestampAge.TotalMinutes);
                return false;
            }

            // Regenerate the signature and compare
            var expectedSignature = GenerateSignature(responseBody, timestamp, nonce, requestId);

            // Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expectedSignature));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate response signature");
            return false;
        }
    }

    public string GenerateNonce()
    {
        // Generate a cryptographically secure random nonce
        var nonceBytes = new byte[16]; // 128 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonceBytes);

        // Combine with timestamp for additional uniqueness
        var timestamp = DateTime.UtcNow.Ticks;
        var nonce = $"{Convert.ToBase64String(nonceBytes)}-{timestamp:X}";

        return nonce;
    }

    public bool IsNonceFresh(string nonce)
    {
        // Check if nonce exists in cache (indicating it was already used)
        if (_nonceCache.TryGetValue(GetNonceCacheKey(nonce), out _))
        {
            _logger.LogWarning("Replay attack detected: Nonce {Nonce} has been used before", nonce);
            return false;
        }

        return true;
    }

    public void RecordNonceUsage(string nonce)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(NonceExpirationMinutes),
            Size = 1 // For cache size limiting
        };

        _nonceCache.Set(GetNonceCacheKey(nonce), true, cacheOptions);

        _logger.LogDebug("Recorded nonce usage: {Nonce}", nonce);
    }

    private string BuildSigningMessage(string responseBody, DateTime timestamp, string nonce, string? requestId)
    {
        // Format: ISO8601Timestamp|Nonce|RequestId|SHA256(Body)
        // We hash the body separately to handle large responses efficiently
        var bodyHash = ComputeBodyHash(responseBody);
        var timestampStr = timestamp.ToString("O"); // ISO 8601 format

        return $"{timestampStr}|{nonce}|{requestId ?? ""}|{bodyHash}";
    }

    private static string ComputeBodyHash(string body)
    {
        using var sha256 = SHA256.Create();
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = sha256.ComputeHash(bodyBytes);
        return Convert.ToBase64String(hash);
    }

    private byte[] GetOrGenerateSigningKey()
    {
        // Try to get key from configuration
        var configuredKey = _configuration["Security:ResponseIntegrity:SigningKey"];

        if (!string.IsNullOrEmpty(configuredKey))
        {
            // If key is base64 encoded
            try
            {
                return Convert.FromBase64String(configuredKey);
            }
            catch
            {
                // If not base64, use it as-is and derive a key
                using var sha256 = SHA256.Create();
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(configuredKey));
            }
        }

        // Fall back to JWT secret key if available (for backward compatibility)
        var jwtSecret = _configuration["Jwt:SecretKey"];
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(jwtSecret));
        }

        // Generate a random key (only for development - logs warning)
        _logger.LogWarning(
            "No signing key configured for response integrity. " +
            "Using randomly generated key. Configure 'Security:ResponseIntegrity:SigningKey' for production.");

        var randomKey = new byte[DefaultKeyLengthBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomKey);
        return randomKey;
    }

    private static string GetNonceCacheKey(string nonce)
    {
        return $"ResponseIntegrity:Nonce:{nonce}";
    }
}

/// <summary>
/// Configuration options for response integrity feature
/// </summary>
public class ResponseIntegrityOptions
{
    /// <summary>
    /// Whether response signing is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The signing key (base64 encoded or plain text)
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// Maximum age of timestamp in minutes before signature is rejected
    /// </summary>
    public int MaxTimestampAgeMinutes { get; set; } = 5;

    /// <summary>
    /// Whether to include response body hash in headers
    /// </summary>
    public bool IncludeBodyHash { get; set; } = true;

    /// <summary>
    /// Paths to exclude from response signing (e.g., health checks, static files)
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/swagger",
        "/favicon.ico"
    };
}
