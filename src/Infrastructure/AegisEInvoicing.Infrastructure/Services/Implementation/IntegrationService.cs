using Ardalis.GuardClauses;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.FIRSAccessPoint.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AegisEInvoicing.Infrastructure.Services.Implementation;

/// <summary>
/// Enterprise-grade implementation of integration service for external API communication
/// Implements resilience patterns, security best practices, and efficient logging
/// </summary>
public sealed class IntegrationService : IIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IntegrationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICacheService _cache;
    private readonly IntegrationServiceOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly IAsyncPolicy<HttpResponseMessage> _circuitBreakerPolicy;

    public IntegrationService(
        HttpClient httpClient,
        ILogger<IntegrationService> logger,
        IServiceProvider serviceProvider,
        ICacheService cache,
        IOptions<IntegrationServiceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Configure resilience policies
        _retryPolicy = CreateRetryPolicy();
        _circuitBreakerPolicy = CreateCircuitBreakerPolicy();
    }


    public async Task<T> GetDataAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(endpoint, nameof(endpoint));

        var correlationId = Guid.CreateVersion7().ToString();
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = GenerateCacheKey(endpoint, typeof(T).Name);
        IntegrationLog? integrationLog = null;

        try
        {
            using var activity = Activity.Current?.Source.StartActivity("IntegrationService.GetData");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetData");
            activity?.SetTag("endpoint", endpoint);

            _logger.LogInformation("Starting data retrieval. CorrelationId: {CorrelationId}, Endpoint: {Endpoint}", correlationId, endpoint);

            // Check cache first
            if (_options.EnableCaching)
            {
                var cachedResult = await _cache.GetAsync<T>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Cache hit for endpoint: {Endpoint}, CorrelationId: {CorrelationId}", endpoint, correlationId);
                    return cachedResult;
                }
            }

            // Create log entry if detailed logging is enabled
            if (_options.EnableDetailedLogging)
            {
                integrationLog = IntegrationLog.Create(
                    "GetData",
                    _options.ServiceName,
                    $"GET {SanitizeEndpoint(endpoint)}",
                    correlationId);
            }

            // Validate endpoint
            ValidateEndpoint(endpoint);

            // Execute with combined resilience policies
            var response = await Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy)
                .ExecuteAsync(async () => await _httpClient.GetAsync(endpoint, cancellationToken));

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await DeserializeResponseAsync<T>(responseContent, correlationId);
                integrationLog?.MarkAsCompleted(SanitizeForLogging(responseContent), true);

                _logger.LogInformation(
                    "Data retrieval completed successfully. CorrelationId: {CorrelationId}, Duration: {Duration}ms, StatusCode: {StatusCode}",
                    correlationId, stopwatch.ElapsedMilliseconds, response.StatusCode);

                // Cache the result
                if (_options.EnableCaching && result != null)
                {
                    await _cache.SetAsync(cacheKey, result, _options.CacheDuration, cancellationToken);
                }

                return result;
            }
            else
            {
                var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                integrationLog?.MarkAsCompleted(SanitizeForLogging(responseContent), false, errorMessage);

                _logger.LogWarning(
                    "Data retrieval failed. CorrelationId: {CorrelationId}, Duration: {Duration}ms, StatusCode: {StatusCode}, Reason: {Reason}",
                    correlationId, stopwatch.ElapsedMilliseconds, response.StatusCode, response.ReasonPhrase);

                throw new IntegrationException($"External service returned error: {errorMessage}", response.StatusCode, correlationId);
            }
        }
        catch (BrokenCircuitException ex)
        {
            stopwatch.Stop();
            integrationLog?.MarkAsCompleted(null, false, "Circuit breaker is open");

            _logger.LogWarning(
                "Circuit breaker is open. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                correlationId, stopwatch.ElapsedMilliseconds);

            throw new IntegrationException("External service is currently unavailable (circuit breaker open)", System.Net.HttpStatusCode.ServiceUnavailable, correlationId, ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            integrationLog?.MarkAsCompleted(null, false, ex.Message);

            _logger.LogError(ex,
                "Unexpected error during data retrieval. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                correlationId, stopwatch.ElapsedMilliseconds);

            throw new IntegrationException("An unexpected error occurred during data retrieval", System.Net.HttpStatusCode.InternalServerError, correlationId, ex);
        }
        finally
        {
            // Save integration log with a new scope to avoid DbContext concurrency issues
            if (integrationLog != null)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    context.IntegrationLogs.Add(integrationLog);
                    await context.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save integration log. CorrelationId: {CorrelationId}", correlationId);
                }
            }
        }
    }

    public async Task<string> SendDataAsync(HttpMethod httpMethod, string url, string data, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(data, nameof(data));
        Guard.Against.NullOrWhiteSpace(apiKey, nameof(apiKey));
        Guard.Against.NullOrWhiteSpace(apiSecret, nameof(apiSecret));
        var correlationId = Guid.CreateVersion7().ToString();
        var stopwatch = Stopwatch.StartNew();
        IntegrationLog? integrationLog = null;

        try
        {
            using var activity = Activity.Current?.Source.StartActivity("IntegrationService.SendDataWithCredentials");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "SendDataWithCredentials");
            _logger.LogInformation("Starting data transmission with custom credentials. CorrelationId: {CorrelationId}", correlationId);

            if (_options.EnableDetailedLogging)
            {
                integrationLog = IntegrationLog.Create(
                    "SendDataWithCredentials",
                    _options.ServiceName,
                    SanitizeForLogging(data),
                    correlationId);
            }

            var sanitizedData = await ValidateAndSanitizeDataAsync(data, cancellationToken);
            await ValidateCircuitBreakerStateAsync(cancellationToken);

            var response = await Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy)
                .ExecuteAsync(async () => await SendDataInternalWithCredentialsAsync(httpMethod, url, sanitizedData, apiKey, apiSecret, correlationId, cancellationToken));

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                integrationLog?.MarkAsCompleted(SanitizeForLogging(responseContent), true);
                _logger.LogInformation(
                    "Data transmission with credentials completed successfully. CorrelationId: {CorrelationId}, Duration: {Duration}ms, StatusCode: {StatusCode}",
                    correlationId, stopwatch.ElapsedMilliseconds, response.StatusCode);

                await SaveIntegrationLogAsync(integrationLog, correlationId);
                return responseContent;
            }
            else
            {
                var deserializedResponse = JsonSerializer.Deserialize<GenericResponse>(responseContent);
                var errorMessage = $"HTTP {response.StatusCode}: {deserializedResponse?.Error?.PublicMessage}";
                integrationLog?.MarkAsCompleted(SanitizeForLogging(responseContent), false, errorMessage);
                _logger.LogWarning(
                    "Data transmission with credentials failed. CorrelationId: {CorrelationId}, Duration: {Duration}ms, StatusCode: {StatusCode}, Reason: {Reason}",
                    correlationId, stopwatch.ElapsedMilliseconds, response.StatusCode, errorMessage);

                await SaveIntegrationLogAsync(integrationLog, correlationId);
                return responseContent;
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            integrationLog?.MarkAsCompleted(null, false, ex.Message);
            _logger.LogError(ex,
                "Unexpected error during data transmission with credentials. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                correlationId, stopwatch.ElapsedMilliseconds);

            await SaveIntegrationLogAsync(integrationLog, correlationId);
            throw new IntegrationException("An unexpected error occurred during data transmission", System.Net.HttpStatusCode.InternalServerError, correlationId, ex);
        }
    }

    public async Task<T> GetDataAsync<T>(string endpoint, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(endpoint, nameof(endpoint));
        Guard.Against.NullOrWhiteSpace(apiKey, nameof(apiKey));
        Guard.Against.NullOrWhiteSpace(apiSecret, nameof(apiSecret));

        var correlationId = Guid.CreateVersion7().ToString();
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = GenerateCacheKey(endpoint, typeof(T).Name);
        IntegrationLog? integrationLog = null;

        try
        {
            using var activity = Activity.Current?.Source.StartActivity("IntegrationService.GetDataWithCredentials");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetDataWithCredentials");
            activity?.SetTag("endpoint", endpoint);

            _logger.LogInformation("Starting data retrieval with custom credentials. CorrelationId: {CorrelationId}, Endpoint: {Endpoint}", correlationId, endpoint);

            if (_options.EnableCaching)
            {
                var cachedResult = await _cache.GetAsync<T>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Cache hit for endpoint: {Endpoint}, CorrelationId: {CorrelationId}", endpoint, correlationId);
                    return cachedResult;
                }
            }

            if (_options.EnableDetailedLogging)
            {
                integrationLog = IntegrationLog.Create(
                    "GetDataWithCredentials",
                    _options.ServiceName,
                    $"GET {SanitizeEndpoint(endpoint)}",
                    correlationId);
            }

            ValidateEndpoint(endpoint);

            var response = await Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy)
                .ExecuteAsync(async () =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    request.Headers.Add("X-API-Key", apiKey);
                    request.Headers.Add("X-API-Secret", apiSecret);
                    request.Headers.Add("X-Correlation-ID", correlationId);
                    request.Headers.Add("X-Request-ID", Guid.CreateVersion7().ToString());
                    return await _httpClient.SendAsync(request, cancellationToken);
                });

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var result = await DeserializeResponseAsync<T>(responseContent, correlationId);
                integrationLog?.MarkAsCompleted(SanitizeForLogging(responseContent), true);

                _logger.LogInformation(
                    "Data retrieval with credentials completed successfully. CorrelationId: {CorrelationId}, Duration: {Duration}ms, StatusCode: {StatusCode}",
                    correlationId, stopwatch.ElapsedMilliseconds, response.StatusCode);

                if (_options.EnableCaching && result != null)
                {
                    await _cache.SetAsync(cacheKey, result, _options.CacheDuration, cancellationToken);
                }

                return result;
            }
            else
            {
                var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                integrationLog?.MarkAsCompleted(SanitizeForLogging(responseContent), false, errorMessage);

                _logger.LogWarning(
                    "Data retrieval with credentials failed. CorrelationId: {CorrelationId}, Duration: {Duration}ms, StatusCode: {StatusCode}, Reason: {Reason}",
                    correlationId, stopwatch.ElapsedMilliseconds, response.StatusCode, response.ReasonPhrase);

                throw new IntegrationException($"External service returned error: {errorMessage}", response.StatusCode, correlationId);
            }
        }
        catch (Exception ex) when (!(ex is IntegrationException))
        {
            stopwatch.Stop();
            integrationLog?.MarkAsCompleted(null, false, ex.Message);

            _logger.LogError(ex,
                "Unexpected error during data retrieval with credentials. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                correlationId, stopwatch.ElapsedMilliseconds);

            throw new IntegrationException("An unexpected error occurred during data retrieval", System.Net.HttpStatusCode.InternalServerError, correlationId, ex);
        }
        finally
        {
            if (integrationLog != null)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    context.IntegrationLogs.Add(integrationLog);
                    await context.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save integration log. CorrelationId: {CorrelationId}", correlationId);
                }
            }
        }
    }

    public async Task<bool> ValidateConnectionAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(apiKey, nameof(apiKey));
        Guard.Against.NullOrWhiteSpace(apiSecret, nameof(apiSecret));

        var correlationId = Guid.CreateVersion7().ToString();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var activity = Activity.Current?.Source.StartActivity("IntegrationService.ValidateConnectionWithCredentials");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "ValidateConnectionWithCredentials");

            _logger.LogInformation("Starting connection validation with custom credentials. CorrelationId: {CorrelationId}", correlationId);

            using var timeoutCts = new CancellationTokenSource(_options.HealthCheckTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using var request = new HttpRequestMessage(HttpMethod.Get, _options.HealthCheckEndpoint);
            request.Headers.Add("X-API-Key", apiKey);
            request.Headers.Add("X-API-Secret", apiSecret);

            var response = await _httpClient.SendAsync(request, combinedCts.Token);
            stopwatch.Stop();

            var isHealthy = response.IsSuccessStatusCode;

            _logger.LogInformation(
                "Connection validation with credentials completed. CorrelationId: {CorrelationId}, Duration: {Duration}ms, IsHealthy: {IsHealthy}, StatusCode: {StatusCode}",
                correlationId, stopwatch.ElapsedMilliseconds, isHealthy, response.StatusCode);

            return isHealthy;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Connection validation with credentials timed out. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                correlationId, stopwatch.ElapsedMilliseconds);
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Connection validation with credentials failed. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                correlationId, stopwatch.ElapsedMilliseconds);
            return false;
        }
    }


    #region Private Helper Methods

    private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode && msg.StatusCode != System.Net.HttpStatusCode.BadRequest && msg.StatusCode != System.Net.HttpStatusCode.Forbidden)
            .WaitAndRetryAsync(
                retryCount: _options.RetryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)), // Jitter
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry attempt {RetryCount} after {Delay}ms. Reason: {Reason}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
                });
    }

    private IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _options.CircuitBreakerThreshold,
                durationOfBreak: _options.CircuitBreakerDuration,
                onBreak: (exception, timespan) =>
                {
                    _logger.LogWarning(
                        "Circuit breaker opened for {Duration}s. Reason: {Reason}",
                        timespan.TotalSeconds, exception.Exception?.Message ?? exception.Result?.ReasonPhrase);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset - service is healthy again");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open - testing service health");
                });
    }

    private async Task<HttpResponseMessage> SendDataInternalWithCredentialsAsync(HttpMethod httpMethod, string url, string data, string apiKey, string apiSecret, string correlationId, CancellationToken cancellationToken)
    {        
        using var request = new HttpRequestMessage(httpMethod, url);
        request.Content = new StringContent(data, Encoding.UTF8, "application/json");

        // Add API credentials
        request.Headers.Add("X-API-Key", apiKey);
        request.Headers.Add("X-API-Secret", apiSecret);

        // Add correlation ID and request ID
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Headers.Add("X-Request-ID", Guid.CreateVersion7().ToString());

        // Add standard headers
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("User-Agent", "AegisEInvoicing/1.0");

        // Add timeout
        using var timeoutCts = new CancellationTokenSource(_options.RequestTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        return await _httpClient.SendAsync(request, combinedCts.Token);
    }



    private async Task<HttpResponseMessage> SendDataInternalAsync(string url, string data, string correlationId, CancellationToken cancellationToken)
    {
        using var content = new StringContent(data, Encoding.UTF8, "application/json");
        
        // Add correlation ID to headers
        content.Headers.Add("X-Correlation-ID", correlationId);
        content.Headers.Add("X-Request-ID", Guid.CreateVersion7().ToString());

        foreach(var header in content.Headers)
        {
            Console.WriteLine($"{header.Key} : {header.Value}");
        }
        
        // Add timeout
        using var timeoutCts = new CancellationTokenSource(_options.RequestTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        return await _httpClient.PostAsync(url, content, combinedCts.Token);
    }

    private Task<string> ValidateAndSanitizeDataAsync(string data, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        try
        {
            using var document = JsonDocument.Parse(data);
            var sanitized = SanitizeJsonElement(document.RootElement);
            

            return Task.FromResult(JsonSerializer.Serialize(sanitized, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format", nameof(data), ex);
        }
    }

    private object? SanitizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(prop => prop.Name, prop => SanitizeJsonElement(prop.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(SanitizeJsonElement).ToArray(),
            JsonValueKind.String => SanitizeString(element.GetString() ?? string.Empty),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
    private string SanitizeTimeOnly(TimeOnly timeOnly)
    {
        return timeOnly.ToString("HH:mm:ss");
    }

    private string SanitizeString(string value)
    {
        // Now we're guaranteed value is not null due to ?? string.Empty above
        if (string.IsNullOrEmpty(value))
            return value;

        // Check if it's a valid phone number first - preserve the format
        if (IsValidPhoneNumber(value))
        {
            return value; // Return phone number unchanged
        }

        // Only try TimeOnly parsing if it's not a phone number
        if (TimeOnly.TryParse(value, out TimeOnly timeOnly))
        {
            return SanitizeTimeOnly(timeOnly);
        }

        var sanitized = value;
        foreach (var pattern in _options.SensitiveDataPatterns)
        {
            sanitized = Regex.Replace(sanitized, pattern, "[REDACTED]", RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    private bool IsValidPhoneNumber(string value)
    {
        // Check if it matches the required format: +234 followed by exactly 10 digits
        if (value.Length != 14)
            return false;

        if (!value.StartsWith("+234"))
            return false;

        // Check if the remaining 10 characters are all digits
        var remainingPart = value.Substring(4);
        return remainingPart.All(char.IsDigit);
    }

    private async Task ValidateCircuitBreakerStateAsync(CancellationToken cancellationToken)
    {
        // This would integrate with your circuit breaker monitoring
        // For now, we'll rely on the Polly circuit breaker
        await Task.CompletedTask;
    }

    private Task<T> DeserializeResponseAsync<T>(string responseContent, string correlationId)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var result = JsonSerializer.Deserialize<T>(responseContent, options);
            if (result == null)
            {
                throw new IntegrationException(
                    "Deserialization resulted in null object", 
                    System.Net.HttpStatusCode.BadGateway, 
                    correlationId);
            }

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new IntegrationException(
                "Failed to deserialize response", 
                System.Net.HttpStatusCode.BadGateway, 
                correlationId, 
                ex);
        }
    }

    private void ValidateEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.RelativeOrAbsolute, out var uri))
            throw new ArgumentException("Invalid endpoint format", nameof(endpoint));

        // Additional endpoint validation can be added here
        if (_options.AllowedEndpoints?.Any() == true && 
            !_options.AllowedEndpoints.Any(allowed => endpoint.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException($"Endpoint '{endpoint}' is not in the allowed list");
        }
    }

    private string GenerateCacheKey(string endpoint, string typeName)
    {
        using var sha256 = SHA256.Create();
        var input = $"{_options.ServiceName}:{endpoint}:{typeName}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return $"integration:{Convert.ToBase64String(hash)[..16]}"; // Truncate for readability
    }

    private async Task CacheResponseIfApplicableAsync(string request, string response, CancellationToken cancellationToken)
    {
        if (!_options.EnableCaching) return;

        // Cache based on request hash for POST operations
        var cacheKey = GenerateCacheKey(request, "POST");
        await _cache.SetAsync(cacheKey, response, _options.CacheDuration, cancellationToken);
    }

    private string SanitizeForLogging(string? data)
    {
        if (string.IsNullOrEmpty(data)) return "[EMPTY]";

        var sanitized = data;
        foreach (var pattern in _options.SensitiveDataPatterns)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, pattern, "[REDACTED]", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Truncate if too long
        return sanitized.Length > _options.MaxLogLength 
            ? $"{sanitized[.._options.MaxLogLength]}... [TRUNCATED]"
            : sanitized;
    }

    private string SanitizeEndpoint(string endpoint)
    {
        // Remove query parameters that might contain sensitive data
        var uri = new Uri(endpoint, UriKind.RelativeOrAbsolute);
        return uri.IsAbsoluteUri ? $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}" : uri.ToString().Split('?')[0];
    }

    private async Task SaveIntegrationLogAsync(IntegrationLog? integrationLog, string correlationId)
    {
        if (integrationLog != null)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                context.IntegrationLogs.Add(integrationLog);
                await context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save integration log. CorrelationId: {CorrelationId}", correlationId);
            }
        }
    }

    #endregion
}