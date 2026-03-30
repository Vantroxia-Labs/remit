using Ardalis.GuardClauses;
using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.Caching;

/// <summary>
/// Redis cache service with in-memory fallback
/// </summary>
public sealed class RedisCacheService(
    IConnectionMultiplexer? redis,
    IMemoryCache memoryCache,
    IOptions<CacheOptions> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IConnectionMultiplexer? _redis = redis;
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    private readonly ILogger<RedisCacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly CacheOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));

        try
        {
            // Try Redis first if available
            if (_redis?.IsConnected == true)
            {
                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(key);

                if (!value.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache hit from Redis for key: {Key}", key);
                    return JsonSerializer.Deserialize<T>(value.ToString());
                }
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis error for key {Key}, falling back to memory cache", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error accessing Redis for key {Key}", key);
        }

        // Fallback to memory cache
        if (_memoryCache.TryGetValue<T>(key, out var memoryCacheValue))
        {
            _logger.LogDebug("Cache hit from memory for key: {Key}", key);
            return memoryCacheValue;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.Null(value, nameof(value));

        var actualExpiry = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        // Always set in memory cache
        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = actualExpiry,
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(key, value, memoryCacheOptions);
        _logger.LogDebug("Value cached in memory for key: {Key}", key);

        try
        {
            // Try to set in Redis if available
            if (_redis?.IsConnected == true)
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(value);

                await db.StringSetAsync(
                    key,
                    json,
                    actualExpiry,
                    When.Always,
                    CommandFlags.FireAndForget);

                _logger.LogDebug("Value cached in Redis for key: {Key}", key);
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to cache in Redis for key {Key}, value exists in memory cache only", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error caching to Redis for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));

        // Remove from memory cache
        _memoryCache.Remove(key);
        _logger.LogDebug("Removed from memory cache: {Key}", key);

        try
        {
            // Try to remove from Redis if available
            if (_redis?.IsConnected == true)
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(key, CommandFlags.FireAndForget);
                _logger.LogDebug("Removed from Redis cache: {Key}", key);
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to remove from Redis for key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(pattern, nameof(pattern));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Remove from Redis if available
            if (_redis?.IsConnected == true)
            {
                var endpoints = _redis.GetEndPoints();
                var server = _redis.GetServer(endpoints.First());

                var keys = server.Keys(pattern: pattern).ToArray();
                if (keys.Any())
                {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync(keys);

                    _logger.LogDebug("Removed {Count} keys from Redis matching pattern: {Pattern}",
                        keys.Length, pattern);
                }
            }

            // Note: MemoryCache doesn't support pattern-based removal
            // Would need to track keys separately for this functionality
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern {Pattern}", pattern);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));

        try
        {
            if (_redis?.IsConnected == true)
            {
                var db = _redis.GetDatabase();
                return await db.KeyExistsAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking key existence in Redis for {Key}", key);
        }

        return _memoryCache.TryGetValue(key, out _);
    }
}