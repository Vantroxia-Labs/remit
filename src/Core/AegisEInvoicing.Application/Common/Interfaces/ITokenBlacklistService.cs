namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for managing revoked/blacklisted JWT tokens to prevent session replay attacks
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a JWT ID (jti) to the blacklist
    /// </summary>
    /// <param name="jti">The JWT ID claim value</param>
    /// <param name="expirationTime">When the token expires (can be removed from blacklist after this)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BlacklistTokenAsync(string jti, DateTimeOffset expirationTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a JWT ID is blacklisted (token has been revoked)
    /// </summary>
    /// <param name="jti">The JWT ID claim value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the token is blacklisted (revoked)</returns>
    Task<bool> IsTokenBlacklistedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired tokens from the blacklist (cleanup operation)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}
