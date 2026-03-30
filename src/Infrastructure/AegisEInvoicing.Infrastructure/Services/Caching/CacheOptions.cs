namespace AegisEInvoicing.Infrastructure.Services.Caching;

/// <summary>
/// Cache configuration options
/// </summary>
public sealed record CacheOptions
{
    public const string SectionName = "Cache";
    public int DefaultExpirationMinutes { get; set; } = 5;
    public int SlidingExpirationMinutes { get; set; } = 2;
    public bool UseDistributedCache { get; set; } = true;
    public string? RedisConnectionString { get; set; }
    public int RedisConnectRetry { get; set; } = 3;
    public int RedisConnectTimeout { get; set; } = 5000;
    public bool AbortOnConnectFail { get; set; } = false;
}