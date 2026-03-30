using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.Implementation;

/// <summary>
/// Redis-based token blacklist service for preventing session replay attacks
/// Stores revoked JWT IDs (jti) in distributed cache
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<TokenBlacklistService> _logger;
    private const string BlacklistKeyPrefix = "token:blacklist:";

    public TokenBlacklistService(
        IDistributedCache cache,
        ILogger<TokenBlacklistService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task BlacklistTokenAsync(string jti, DateTimeOffset expirationTime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            _logger.LogWarning("Attempted to blacklist token with null or empty JTI");
            return;
        }

        try
        {
            var key = GetBlacklistKey(jti);
            var timeToExpiry = expirationTime - DateTimeOffset.UtcNow;

            // Only store if token hasn't already expired
            if (timeToExpiry.TotalSeconds > 0)
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = expirationTime
                };

                var value = JsonSerializer.Serialize(new
                {
                    RevokedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = expirationTime
                });

                await _cache.SetStringAsync(key, value, options, cancellationToken);
                _logger.LogInformation("Token with JTI {Jti} added to blacklist, expires at {ExpirationTime}",
                    jti, expirationTime);
            }
            else
            {
                _logger.LogDebug("Token with JTI {Jti} already expired, not adding to blacklist", jti);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token with JTI {Jti}", jti);
            // Don't throw - failing to blacklist shouldn't break logout flow
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        try
        {
            var key = GetBlacklistKey(jti);
            var value = await _cache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token is blacklisted for JTI {Jti}", jti);
            // Fail open - if cache is down, allow the request (token will still have lifetime validation)
            return false;
        }
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        // Redis automatically removes expired keys, so this is a no-op
        // Included for interface completeness and potential future implementation with other cache providers
        await Task.CompletedTask;
        _logger.LogDebug("Token blacklist cleanup completed (automatic with Redis)");
    }

    private static string GetBlacklistKey(string jti) => $"{BlacklistKeyPrefix}{jti}";
}
