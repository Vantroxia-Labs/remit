using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisEInvoicing.Portal.API.Models;

namespace AegisEInvoicing.Portal.API.Middleware;

/// <summary>
/// Middleware to decrypt encrypted request payloads for sensitive endpoints
/// Uses AES-256-CBC encryption with a shared key
/// </summary>
public class PayloadDecryptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PayloadDecryptionMiddleware> _logger;
    private readonly byte[] _encryptionKey;

    /// <summary>
    /// Endpoints that require payload decryption when X-Encrypted header is present
    /// </summary>
    private static readonly string[] ProtectedPaths = new[]
    {
        "/api/v1/auth/login",
        "/api/v2/auth/login",
        "/api/v1/auth/forgot-password",
        "/api/v2/auth/forgot-password",
        "/api/v1/profile/change-password",
        "/api/v2/profile/change-password",
    };

    public PayloadDecryptionMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<PayloadDecryptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // Priority: env var (from server .env) > appsettings.json config
        // The server's .env sets PAYLOAD_ENCRYPTION_KEY (flat name), which must
        // take precedence over any stale value in appsettings.json.
        var envKey = Environment.GetEnvironmentVariable("PAYLOAD_ENCRYPTION_KEY");
        var configKey = configuration["PayloadEncryption:Key"];

        string? key;
        string keySource;

        if (!string.IsNullOrEmpty(envKey))
        {
            key = envKey;
            keySource = "PAYLOAD_ENCRYPTION_KEY env var";
        }
        else if (!string.IsNullOrEmpty(configKey))
        {
            key = configKey;
            keySource = "PayloadEncryption:Key config";
        }
        else
        {
            key = null;
            keySource = "none";
        }

        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Payload encryption key not configured. Encrypted payloads will fail to decrypt.");
            _encryptionKey = Array.Empty<byte>();
        }
        else
        {
            _encryptionKey = Convert.FromBase64String(key);

            if (_encryptionKey.Length != 32)
            {
                throw new InvalidOperationException(
                    $"Payload encryption key must be 32 bytes (256 bits). Got {_encryptionKey.Length} bytes.");
            }

            // Log safe key fingerprint for debugging key mismatch issues
            var keyFingerprint = key.Length >= 8 ? key[..8] : key;
            _logger.LogInformation(
                "Payload encryption key loaded from {Source}. Fingerprint: {Fingerprint}..., Length: {Length} bytes",
                keySource, keyFingerprint, _encryptionKey.Length);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a POST request to a protected endpoint
        if (context.Request.Method == HttpMethod.Post.Method &&
            IsProtectedEndpoint(context.Request.Path))
        {
            // Protected endpoints MUST have encrypted payloads
            var hasEncryptedHeader = context.Request.Headers.TryGetValue("X-Encrypted", out var encryptedHeader) &&
                encryptedHeader.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

            if (!hasEncryptedHeader)
            {
                _logger.LogWarning(
                    "Rejected unencrypted request to protected endpoint {Path}. X-Encrypted header missing or not 'true'.",
                    context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Encrypted payload required",
                    message = "This endpoint requires an encrypted payload. Please encrypt the request body."
                });
                return;
            }

            if (_encryptionKey.Length == 0)
            {
                _logger.LogError("Encrypted request received but no encryption key is configured");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Server encryption configuration error" });
                return;
            }

            try
            {
                // Enable buffering so we can read the body
                context.Request.EnableBuffering();

                // Read and decrypt the request body
                var decryptedBody = await DecryptRequestBodyAsync(context.Request.Body);

                // Replace the request body with the decrypted content
                var decryptedBytes = Encoding.UTF8.GetBytes(decryptedBody);
                context.Request.Body = new MemoryStream(decryptedBytes);
                context.Request.Body.Position = 0;

                // Update content length (must be byte count, not string character count)
                context.Request.ContentLength = decryptedBytes.Length;

                _logger.LogDebug("Successfully decrypted request payload for {Path}", context.Request.Path);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid encrypted payload format for {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid encrypted payload format" });
                return;
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt payload for {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Failed to decrypt payload" });
                return;
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid Base64 in encrypted payload for {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid encrypted payload encoding" });
                return;
            }
        }

        await _next(context);
    }

    private bool IsProtectedEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        return ProtectedPaths.Any(p => pathValue.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> DecryptRequestBodyAsync(Stream body)
    {
        using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
        var encryptedJson = await reader.ReadToEndAsync();

        var payload = JsonSerializer.Deserialize<EncryptedPayload>(encryptedJson)
            ?? throw new JsonException("Failed to deserialize encrypted payload");

        if (string.IsNullOrEmpty(payload.Data) || string.IsNullOrEmpty(payload.Iv))
        {
            throw new JsonException("Encrypted payload missing required fields (data, iv)");
        }

        var iv = Convert.FromBase64String(payload.Iv);
        var ciphertext = Convert.FromBase64String(payload.Data);

        if (iv.Length != 16)
        {
            throw new CryptographicException($"IV must be 16 bytes. Got {iv.Length} bytes.");
        }

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}

/// <summary>
/// Extension methods for PayloadDecryptionMiddleware
/// </summary>
public static class PayloadDecryptionMiddlewareExtensions
{
    public static IApplicationBuilder UsePayloadDecryption(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PayloadDecryptionMiddleware>();
    }
}
