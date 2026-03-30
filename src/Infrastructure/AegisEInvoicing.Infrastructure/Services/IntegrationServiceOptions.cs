namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Configuration options for the IntegrationService
/// </summary>
public sealed class IntegrationServiceOptions
{
    public const string SectionName = "IntegrationService";

    /// <summary>
    /// Name of the external service
    /// </summary>
    public string ServiceName { get; set; } = "ExternalAPI";

    /// <summary>
    /// Data submission endpoint
    /// </summary>
    public string DataEndpoint { get; set; } = "api/data";

    /// <summary>
    /// Health check endpoint
    /// </summary>
    public string HealthCheckEndpoint { get; set; } = "api";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Health check timeout in seconds
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Circuit breaker failure threshold
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker open duration
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable caching for GET operations
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// Cache duration for successful responses
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enable detailed logging (includes request/response data)
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// Maximum length for log entries
    /// </summary>
    public int MaxLogLength { get; set; } = 1000;

    /// <summary>
    /// Regex patterns for sensitive data that should be redacted from logs
    /// </summary>
    public List<string> SensitiveDataPatterns { get; set; } =
    [
        @"(?i)(password|pwd|secret|key|token|auth)[""\s]*[:=][""\s]*[^""\s,}]+",
        @"(?i)(ssn|social[-\s]?security|tax[-\s]?id)[""\s]*[:=][""\s]*[\d-]+",
        @"(?i)(credit[-\s]?card|cc|card[-\s]?number)[""\s]*[:=][""\s]*[\d\s-]+",
        @"(?i)(email|e[-\s]?mail)[""\s]*[:=][""\s]*[^""\s,}]+@[^""\s,}]+",
        @"(?i)(phone|mobile|tel)[""\s]*[:=][""\s]*[\+\d\s()-]+"
    ];

    /// <summary>
    /// List of allowed endpoints (for security)
    /// </summary>
    public List<string>? AllowedEndpoints { get; set; }

    /// <summary>
    /// Enable distributed tracing
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Custom headers to add to requests
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = [];
}