namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries that can be cached
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Gets the cache key for this query
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets whether to bypass cache for this query
    /// </summary>
    bool BypassCache { get; }

    /// <summary>
    /// Gets the sliding expiration time for cache
    /// </summary>
    TimeSpan? SlidingExpiration { get; }

    /// <summary>
    /// Gets the absolute expiration time for cache
    /// </summary>
    DateTimeOffset? AbsoluteExpiration { get; }

    /// <summary>
    /// Gets the absolute expiration in minutes (fallback)
    /// </summary>
    int? AbsoluteExpirationMinutes { get; }
}