using AegisEInvoicing.Domain.Entities.UserManagement;
using System.Security.Claims;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for JWT token generation and validation
/// Enterprise-level security with proper claims management
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token with user claims, merchant/branch context, and permissions
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> permissions, IEnumerable<string> roles, Guid? sessionId = null);

    /// <summary>
    /// Generates a cryptographically secure refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates and extracts claims from an expired token (for refresh scenarios)
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>
    /// Validates if a token is properly formatted and not tampered with
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    /// Gets the expiration time for access tokens
    /// </summary>
    TimeSpan AccessTokenLifetime { get; }

    /// <summary>
    /// Gets the expiration time for refresh tokens
    /// </summary>
    TimeSpan RefreshTokenLifetime { get; }

    /// <summary>
    /// Generates an encrypted JWT access token (entire token is AES encrypted)
    /// </summary>
    Task<string> GenerateEncryptedAccessTokenAsync(User user, IEnumerable<string> permissions, IEnumerable<string> roles, Guid? sessionId = null);

    /// <summary>
    /// Decrypts an encrypted access token to get the original JWT
    /// </summary>
    Task<string> DecryptAccessTokenAsync(string encryptedToken);
}