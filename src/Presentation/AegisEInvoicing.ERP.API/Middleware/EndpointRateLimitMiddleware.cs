using System.Collections.Concurrent;

namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Advanced rate limiting middleware with endpoint-specific limits and burst detection.
/// Addresses request flooding vulnerability
/// Implements sliding window counter algorithm for precise rate limiting.
/// </summary>
public class EndpointRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EndpointRateLimitMiddleware> _logger;
    private readonly IConfiguration _configuration;

    // In-memory store for rate limit tracking (use Redis in production)
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();

    public EndpointRateLimitMiddleware(
        RequestDelegate next,
        ILogger<EndpointRateLimitMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var isEnabled = _configuration.GetValue<bool>("EndpointRateLimit:Enabled", true);

        if (!isEnabled)
        {
            await _next(context);
            return;
        }

        var endpoint = GetEndpointKey(context);
        var rateLimitConfig = GetRateLimitConfig(context);

        if (rateLimitConfig == null)
        {
            await _next(context);
            return;
        }

        // Create unique key: userId + endpoint + IP
        var userId = context.User?.Identity?.Name ?? "anonymous";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = $"{userId}:{endpoint}:{ipAddress}";

        var rateLimitInfo = _rateLimitStore.GetOrAdd(rateLimitKey, _ => new RateLimitInfo());

        // Check rate limits and update state (synchronous, inside lock)
        bool isRateLimited;
        bool isBurstAttack = false;  // Initialize here
        int maxRequests;
        int remaining = 0;  // Initialize here
        long resetTimestamp = 0;  // Initialize here
        int retryAfter;
        int requestCount;
        int burstCount = 0;  // Initialize here

        lock (rateLimitInfo.Lock)
        {
            var now = DateTime.UtcNow;

            // Remove old requests outside the window
            rateLimitInfo.RequestTimestamps.RemoveAll(ts =>
                now - ts > TimeSpan.FromSeconds(rateLimitConfig.WindowSeconds));

            requestCount = rateLimitInfo.RequestTimestamps.Count;
            maxRequests = rateLimitConfig.MaxRequests;

            // Check if limit exceeded
            isRateLimited = requestCount >= maxRequests;

            if (isRateLimited)
            {
                resetTimestamp = new DateTimeOffset(rateLimitInfo.RequestTimestamps.First()
                    .AddSeconds(rateLimitConfig.WindowSeconds)).ToUnixTimeSeconds();
                retryAfter = rateLimitConfig.RetryAfterSeconds;
                remaining = 0;
            }
            else
            {
                // Detect burst attacks (too many requests in short burst)
                var burstWindowSeconds = rateLimitConfig.BurstWindowSeconds;
                var burstLimit = rateLimitConfig.BurstLimit;
                burstCount = rateLimitInfo.RequestTimestamps
                    .Count(ts => now - ts <= TimeSpan.FromSeconds(burstWindowSeconds));

                isBurstAttack = burstCount >= burstLimit;

                if (!isBurstAttack)
                {
                    // Add current request
                    rateLimitInfo.RequestTimestamps.Add(now);
                    remaining = maxRequests - rateLimitInfo.RequestTimestamps.Count;
                    resetTimestamp = new DateTimeOffset(now.AddSeconds(rateLimitConfig.WindowSeconds)).ToUnixTimeSeconds();
                    retryAfter = rateLimitConfig.RetryAfterSeconds;
                }
                else
                {
                    remaining = 0;
                    resetTimestamp = 0;
                    retryAfter = rateLimitConfig.RetryAfterSeconds;
                }
            }
        }

        // Handle rate limit exceeded (outside lock, can now use await)
        if (isRateLimited)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {User} on {Endpoint} from IP {IP}. " +
                "Requests: {Count}/{Max} in {Window}s",
                userId,
                endpoint,
                ipAddress,
                requestCount,
                maxRequests,
                rateLimitConfig.WindowSeconds);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfter.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Reset"] = resetTimestamp.ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                Success = false,
                Message = $"Rate limit exceeded. Maximum {maxRequests} requests per {rateLimitConfig.WindowSeconds} seconds allowed.",
                ErrorCode = "RATE_LIMIT_EXCEEDED",
                RetryAfter = retryAfter,
                Timestamp = DateTime.UtcNow
            });

            return;
        }

        // Handle burst attack (outside lock, can now use await)
        if (isBurstAttack)
        {
            _logger.LogWarning(
                "Burst attack detected for {User} on {Endpoint} from IP {IP}. " +
                "{Count} requests in {Burst}s",
                userId,
                endpoint,
                ipAddress,
                burstCount,
                rateLimitConfig.BurstWindowSeconds);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                Success = false,
                Message = "Too many requests in a short time. Please slow down.",
                ErrorCode = "BURST_ATTACK_DETECTED",
                Timestamp = DateTime.UtcNow
            });

            return;
        }

        // Set rate limit headers (successful request)
        context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetTimestamp.ToString();

        await _next(context);

        // Cleanup old entries periodically
        CleanupOldEntries();
    }
    private static string GetEndpointKey(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();
        return $"{method}:{path}";
    }

    private RateLimitConfig? GetRateLimitConfig(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();

        // Invoice creation - strictest limits
        if ((path.Contains("/firs/create-invoice") || path.Contains("/systemintegrator/create-invoice"))
            && method == "POST")
        {
            return new RateLimitConfig
            {
                MaxRequests = _configuration.GetValue<int>("EndpointRateLimit:InvoiceCreate:MaxRequests", 10),
                WindowSeconds = _configuration.GetValue<int>("EndpointRateLimit:InvoiceCreate:WindowSeconds", 60),
                BurstLimit = _configuration.GetValue<int>("EndpointRateLimit:InvoiceCreate:BurstLimit", 3),
                BurstWindowSeconds = _configuration.GetValue<int>("EndpointRateLimit:InvoiceCreate:BurstWindowSeconds", 5),
                RetryAfterSeconds = 60
            };
        }

        // Validate/Sign/Transmit operations - moderate limits
        if ((path.Contains("/validate") || path.Contains("/sign") || path.Contains("/transmit"))
            && method == "POST")
        {
            return new RateLimitConfig
            {
                MaxRequests = _configuration.GetValue<int>("EndpointRateLimit:InvoiceOperations:MaxRequests", 30),
                WindowSeconds = _configuration.GetValue<int>("EndpointRateLimit:InvoiceOperations:WindowSeconds", 60),
                BurstLimit = _configuration.GetValue<int>("EndpointRateLimit:InvoiceOperations:BurstLimit", 10),
                BurstWindowSeconds = _configuration.GetValue<int>("EndpointRateLimit:InvoiceOperations:BurstWindowSeconds", 10),
                RetryAfterSeconds = 30
            };
        }

        // Exclude health check and swagger from rate limiting
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) || 
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Global fallback for all other requests
        return new RateLimitConfig
        {
            MaxRequests = _configuration.GetValue<int>("EndpointRateLimit:Global:MaxRequests", 100),
            WindowSeconds = _configuration.GetValue<int>("EndpointRateLimit:Global:WindowSeconds", 60),
            BurstLimit = _configuration.GetValue<int>("EndpointRateLimit:Global:BurstLimit", 50),
            BurstWindowSeconds = _configuration.GetValue<int>("EndpointRateLimit:Global:BurstWindowSeconds", 10),
            RetryAfterSeconds = 60
        };
    }

    private static void CleanupOldEntries()
    {
        // Periodically cleanup entries older than 1 hour
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var keysToRemove = _rateLimitStore
            .Where(kvp => kvp.Value.RequestTimestamps.All(ts => ts < cutoff))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _rateLimitStore.TryRemove(key, out _);
        }
    }

    private class RateLimitInfo
    {
        public List<DateTime> RequestTimestamps { get; } = new();
        public object Lock { get; } = new();
    }

    private class RateLimitConfig
    {
        public int MaxRequests { get; set; }
        public int WindowSeconds { get; set; }
        public int BurstLimit { get; set; }
        public int BurstWindowSeconds { get; set; }
        public int RetryAfterSeconds { get; set; }
    }
}

public static class EndpointRateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseEndpointRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EndpointRateLimitMiddleware>();
    }
}