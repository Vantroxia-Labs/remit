using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace AegisEInvoicing.ERP.API.Security;

/// <summary>
/// Service for validating server-side actions to prevent reliance on client-side responses.
/// Addresses VAPT finding: Response tampering - ensures all actions are validated server-side.
///
/// Key security features:
/// 1. Action tokens - cryptographically signed tokens for state-changing operations
/// 2. Idempotency tracking - prevents duplicate action execution
/// 3. State validation - verifies current state before allowing transitions
/// 4. Audit logging - records all action attempts for forensics
/// </summary>
public interface IServerSideActionValidator
{
    /// <summary>
    /// Generates a secure action token for a state-changing operation.
    /// The token must be submitted with the action request.
    /// </summary>
    /// <param name="actionType">Type of action (e.g., "ApproveInvoice", "SubmitPayment")</param>
    /// <param name="resourceId">ID of the resource being acted upon</param>
    /// <param name="userId">ID of the user performing the action</param>
    /// <param name="expectedState">Expected current state of the resource</param>
    /// <param name="additionalClaims">Additional claims to embed in the token</param>
    /// <returns>A secure action token</returns>
    ActionToken GenerateActionToken(
        string actionType,
        string resourceId,
        string userId,
        string? expectedState = null,
        Dictionary<string, string>? additionalClaims = null);

    /// <summary>
    /// Validates an action token and verifies it hasn't been used before.
    /// </summary>
    /// <param name="token">The action token to validate</param>
    /// <param name="expectedActionType">Expected action type</param>
    /// <param name="expectedResourceId">Expected resource ID</param>
    /// <param name="expectedUserId">Expected user ID</param>
    /// <returns>Validation result with details</returns>
    ActionValidationResult ValidateActionToken(
        string token,
        string expectedActionType,
        string expectedResourceId,
        string expectedUserId);

    /// <summary>
    /// Marks an action token as consumed (for idempotency)
    /// </summary>
    /// <param name="tokenId">The token ID to mark as consumed</param>
    void ConsumeToken(string tokenId);

    /// <summary>
    /// Validates that a resource is in the expected state before allowing an action.
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">ID of the resource</param>
    /// <param name="expectedState">Expected state</param>
    /// <param name="actualStateProvider">Function to get actual state</param>
    /// <returns>True if state matches, false otherwise</returns>
    Task<StateValidationResult> ValidateResourceStateAsync(
        string resourceType,
        string resourceId,
        string expectedState,
        Func<Task<string>> actualStateProvider);
}

/// <summary>
/// Represents a secure action token
/// </summary>
public class ActionToken
{
    /// <summary>
    /// Unique token identifier
    /// </summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>
    /// The signed token value to be sent to the client
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Type of action this token authorizes
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Resource this token applies to
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;
}

/// <summary>
/// Result of action token validation
/// </summary>
public class ActionValidationResult
{
    public bool IsValid { get; set; }
    public string? TokenId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ActionType { get; set; }
    public string? ResourceId { get; set; }
    public string? UserId { get; set; }
    public string? ExpectedState { get; set; }
    public Dictionary<string, string>? AdditionalClaims { get; set; }

    public static ActionValidationResult Success(
        string tokenId,
        string actionType,
        string resourceId,
        string userId,
        string? expectedState,
        Dictionary<string, string>? additionalClaims) => new()
    {
        IsValid = true,
        TokenId = tokenId,
        ActionType = actionType,
        ResourceId = resourceId,
        UserId = userId,
        ExpectedState = expectedState,
        AdditionalClaims = additionalClaims
    };

    public static ActionValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Result of state validation
/// </summary>
public class StateValidationResult
{
    public bool IsValid { get; set; }
    public string? ExpectedState { get; set; }
    public string? ActualState { get; set; }
    public string? ErrorMessage { get; set; }

    public static StateValidationResult Success(string expectedState, string actualState) => new()
    {
        IsValid = true,
        ExpectedState = expectedState,
        ActualState = actualState
    };

    public static StateValidationResult Failure(string expectedState, string actualState, string message) => new()
    {
        IsValid = false,
        ExpectedState = expectedState,
        ActualState = actualState,
        ErrorMessage = message
    };
}

/// <summary>
/// Implementation of server-side action validator
/// </summary>
public class ServerSideActionValidator : IServerSideActionValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServerSideActionValidator> _logger;
    private readonly IMemoryCache _tokenCache;
    private readonly byte[] _signingKey;

    private const int DefaultTokenExpirationMinutes = 15;
    private const int TokenIdLength = 16;

    public ServerSideActionValidator(
        IConfiguration configuration,
        ILogger<ServerSideActionValidator> logger,
        IMemoryCache tokenCache)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenCache = tokenCache;
        _signingKey = GetSigningKey();
    }

    public ActionToken GenerateActionToken(
        string actionType,
        string resourceId,
        string userId,
        string? expectedState = null,
        Dictionary<string, string>? additionalClaims = null)
    {
        // Generate unique token ID
        var tokenId = GenerateTokenId();

        // Set expiration
        var expirationMinutes = _configuration.GetValue(
            "Security:ActionValidation:TokenExpirationMinutes",
            DefaultTokenExpirationMinutes);
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Build token payload
        var payload = new ActionTokenPayload
        {
            TokenId = tokenId,
            ActionType = actionType,
            ResourceId = resourceId,
            UserId = userId,
            ExpectedState = expectedState,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            AdditionalClaims = additionalClaims ?? new Dictionary<string, string>()
        };

        // Serialize and sign
        var payloadJson = JsonSerializer.Serialize(payload);
        var signature = ComputeSignature(payloadJson);

        // Combine payload and signature
        var token = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))}.{signature}";

        _logger.LogInformation(
            "Generated action token {TokenId} for {ActionType} on resource {ResourceId} by user {UserId}",
            tokenId, actionType, resourceId, userId);

        return new ActionToken
        {
            TokenId = tokenId,
            Token = token,
            ExpiresAt = expiresAt,
            ActionType = actionType,
            ResourceId = resourceId
        };
    }

    public ActionValidationResult ValidateActionToken(
        string token,
        string expectedActionType,
        string expectedResourceId,
        string expectedUserId)
    {
        try
        {
            // Split token into payload and signature
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid token format - missing parts");
                return ActionValidationResult.Failure("Invalid token format");
            }

            var payloadBase64 = parts[0];
            var providedSignature = parts[1];

            // Decode payload
            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(payloadBase64));

            // Verify signature
            var expectedSignature = ComputeSignature(payloadJson);
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedSignature),
                Encoding.UTF8.GetBytes(expectedSignature)))
            {
                _logger.LogWarning("Token signature validation failed - possible tampering attempt");
                return ActionValidationResult.Failure("Invalid token signature");
            }

            // Deserialize payload
            var payload = JsonSerializer.Deserialize<ActionTokenPayload>(payloadJson);
            if (payload == null)
            {
                return ActionValidationResult.Failure("Invalid token payload");
            }

            // Check expiration
            if (DateTime.UtcNow > payload.ExpiresAt)
            {
                _logger.LogWarning("Token {TokenId} has expired", payload.TokenId);
                return ActionValidationResult.Failure("Token has expired");
            }

            // Verify action type
            if (!string.Equals(payload.ActionType, expectedActionType, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Token action type mismatch: expected {Expected}, got {Actual}",
                    expectedActionType, payload.ActionType);
                return ActionValidationResult.Failure("Action type mismatch");
            }

            // Verify resource ID
            if (!string.Equals(payload.ResourceId, expectedResourceId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Token resource ID mismatch: expected {Expected}, got {Actual}",
                    expectedResourceId, payload.ResourceId);
                return ActionValidationResult.Failure("Resource ID mismatch");
            }

            // Verify user ID
            if (!string.Equals(payload.UserId, expectedUserId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Token user ID mismatch: expected {Expected}, got {Actual}",
                    expectedUserId, payload.UserId);
                return ActionValidationResult.Failure("User ID mismatch");
            }

            // Check if token has been consumed (idempotency check)
            var consumedKey = GetConsumedTokenKey(payload.TokenId);
            if (_tokenCache.TryGetValue(consumedKey, out _))
            {
                _logger.LogWarning("Token {TokenId} has already been consumed - possible replay attack", payload.TokenId);
                return ActionValidationResult.Failure("Token has already been used");
            }

            _logger.LogInformation(
                "Successfully validated action token {TokenId} for {ActionType}",
                payload.TokenId, payload.ActionType);

            return ActionValidationResult.Success(
                payload.TokenId,
                payload.ActionType,
                payload.ResourceId,
                payload.UserId,
                payload.ExpectedState,
                payload.AdditionalClaims);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to decode token - invalid format");
            return ActionValidationResult.Failure("Invalid token format");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse token payload");
            return ActionValidationResult.Failure("Invalid token payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating action token");
            return ActionValidationResult.Failure("Token validation failed");
        }
    }

    public void ConsumeToken(string tokenId)
    {
        var consumedKey = GetConsumedTokenKey(tokenId);

        // Store consumed token for longer than expiration to prevent replay
        var expirationMinutes = _configuration.GetValue(
            "Security:ActionValidation:TokenExpirationMinutes",
            DefaultTokenExpirationMinutes);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes * 2),
            Size = 1
        };

        _tokenCache.Set(consumedKey, DateTime.UtcNow, cacheOptions);

        _logger.LogInformation("Consumed action token {TokenId}", tokenId);
    }

    public async Task<StateValidationResult> ValidateResourceStateAsync(
        string resourceType,
        string resourceId,
        string expectedState,
        Func<Task<string>> actualStateProvider)
    {
        try
        {
            var actualState = await actualStateProvider();

            if (string.Equals(actualState, expectedState, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "State validation passed for {ResourceType} {ResourceId}: {State}",
                    resourceType, resourceId, actualState);

                return StateValidationResult.Success(expectedState, actualState);
            }

            _logger.LogWarning(
                "State validation failed for {ResourceType} {ResourceId}: expected {Expected}, actual {Actual}",
                resourceType, resourceId, expectedState, actualState);

            return StateValidationResult.Failure(
                expectedState,
                actualState,
                $"Resource state has changed. Expected '{expectedState}' but found '{actualState}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error validating state for {ResourceType} {ResourceId}",
                resourceType, resourceId);

            return StateValidationResult.Failure(
                expectedState,
                "unknown",
                "Failed to retrieve current resource state");
        }
    }

    private string GenerateTokenId()
    {
        var bytes = new byte[TokenIdLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private string ComputeSignature(string data)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    private byte[] GetSigningKey()
    {
        var configuredKey = _configuration["Security:ActionValidation:SigningKey"];

        if (!string.IsNullOrEmpty(configuredKey))
        {
            try
            {
                return Convert.FromBase64String(configuredKey);
            }
            catch
            {
                using var sha256 = SHA256.Create();
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(configuredKey));
            }
        }

        // Fall back to JWT secret
        var jwtSecret = _configuration["Jwt:SecretKey"];
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(jwtSecret + ":ActionValidation"));
        }

        _logger.LogWarning(
            "No signing key configured for action validation. " +
            "Using randomly generated key. Configure 'Security:ActionValidation:SigningKey' for production.");

        var randomKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomKey);
        return randomKey;
    }

    private static string GetConsumedTokenKey(string tokenId)
    {
        return $"ActionValidation:Consumed:{tokenId}";
    }

    /// <summary>
    /// Internal payload structure for action tokens
    /// </summary>
    private class ActionTokenPayload
    {
        public string TokenId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? ExpectedState { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public Dictionary<string, string> AdditionalClaims { get; set; } = new();
    }
}
