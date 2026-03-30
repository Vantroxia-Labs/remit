using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Infrastructure.Services.Licensing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.Licensing;

/// <summary>
/// HTTP client implementation for the Licensing Service
/// Implements fail-open strategy for server errors (500)
/// </summary>
public class LicensingService : ILicensingService
{
    private readonly HttpClient _httpClient;
    private readonly LicensingServiceOptions _options;
    private readonly ILogger<LicensingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ITelemetryService? _telemetryService;

    public LicensingService(
        HttpClient httpClient,
        IOptions<LicensingServiceOptions> options,
        ILogger<LicensingService> logger,
        ITelemetryService? telemetryService = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryService = telemetryService;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        // Ensure BaseUrl ends with "/" for proper Uri concatenation
        var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = _options.RequestTimeout;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AegisEInvoicing/1.0");
        
        _logger.LogInformation("LicensingService configured with BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
    }

    /// <summary>
    /// Builds the full endpoint URL by combining base address with relative endpoint.
    /// Removes leading "/" from endpoint to ensure proper Uri concatenation.
    /// </summary>
    private string BuildEndpoint(string endpoint)
    {
        // Remove leading "/" to make it truly relative (avoids replacing base path)
        var relativeEndpoint = endpoint.TrimStart('/');
        return relativeEndpoint;
    }



    public async Task<LicenseGenerationResult> GenerateLicenseAsync(
        string businessId,
        DateTime expiryDate,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        
        try
        {
            var request = new GenerateLicenseRequest
            {
                AppName = "EInvoicing",
                AppVersion = "V1",
                ClientId = businessId,
                ExpiryDate = expiryDate
            };

            _logger.LogInformation(
                "Generating license for business {BusinessId}, expires: {ExpiryDate}",
                businessId, expiryDate);

            var response = await PostAsync<GenerateLicenseRequest, GenerateLicenseResponse>(
                _options.GenerateLicenseEndpoint,
                request,
                cancellationToken);

            if (response.Status == 200)
            {
                success = true;
                _logger.LogInformation(
                    "License generated successfully for business {BusinessId}",
                    businessId);
            }
            else
            {
                _logger.LogError(
                    "License generation failed for business {BusinessId}: {Message}",
                    businessId, response.Message);
            }

            // Track license generation
            var duration = DateTime.UtcNow - startTime;
            _telemetryService?.TrackDependency(
                "HTTP",
                "LicensingService",
                "GenerateLicense",
                duration,
                success,
                response.Status);

            if (Guid.TryParse(businessId, out var businessGuid))
            {
                _telemetryService?.TrackLicenseGenerated(businessGuid, expiryDate, success);
            }

            return new LicenseGenerationResult
            {
                Status = response.Status,
                LicenseKey = response.Data,
                Message = response.Message
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogError(ex,
                "Unexpected error while generating license for business {BusinessId}",
                businessId);

            // Track failed license generation
            _telemetryService?.TrackDependency(
                "HTTP",
                "LicensingService",
                "GenerateLicense",
                duration,
                false,
                500,
                ex.Message);

            return new LicenseGenerationResult
            {
                Status = 500,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<LicenseKeyValidationResult> ValidateLicenseKeyAsync(
        string licenseKey,
        bool failOpen = false,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();

        try
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                _logger.LogWarning("[{RequestId}] License key validation called with empty key", requestId);
                return LicenseKeyValidationResult.Failure("License key is required");
            }

            _logger.LogInformation(
                "[{RequestId}] Validating license key (FailOpen: {FailOpen})",
                requestId, failOpen);

            // Call GET /validate-license-key/{key} endpoint
            var endpoint = $"{_options.ValidateLicenseEndpoint}/{licenseKey}";
            var (response, statusCode) = await GetAsync<ValidateLicenseKeyResponse>(endpoint, cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            // Handle server errors based on fail-open strategy
            if (statusCode == HttpStatusCode.InternalServerError ||
                statusCode == HttpStatusCode.ServiceUnavailable ||
                statusCode == HttpStatusCode.GatewayTimeout)
            {
                _telemetryService?.TrackDependency(
                    "HTTP",
                    "LicensingService",
                    "ValidateLicenseKey",
                    duration,
                    false,
                    (int)statusCode);

                if (failOpen)
                {
                    _logger.LogWarning(
                        "[{RequestId}] Licensing service error ({StatusCode}). FAIL-OPEN: Allowing operation.",
                        requestId, statusCode);

                    return LicenseKeyValidationResult.FailOpen(
                        $"Licensing service returned {statusCode}");
                }
                else
                {
                    _logger.LogError(
                        "[{RequestId}] Licensing service error ({StatusCode}). FAIL-CLOSED: Blocking operation.",
                        requestId, statusCode);

                    return LicenseKeyValidationResult.Failure(
                        $"License service unavailable: {statusCode}",
                        (int)statusCode);
                }
            }

            // Handle successful validation
            if (statusCode == HttpStatusCode.OK && response?.Data != null)
            {
                _logger.LogInformation(
                    "[{RequestId}] License key validated successfully. ClientId: {ClientId}, Status: {Status}",
                    requestId, response.Data.ClientId, response.Data.Status);

                _telemetryService?.TrackDependency(
                    "HTTP",
                    "LicensingService",
                    "ValidateLicenseKey",
                    duration,
                    true,
                    200);

                if (Guid.TryParse(response.Data.ClientId, out var businessId))
                {
                    _telemetryService?.TrackLicenseValidated(businessId, true, false);
                }

                return LicenseKeyValidationResult.Success(
                    response.Data.ClientId!,
                    response.Data.AppName!,
                    response.Data.AppVersion!,
                    DateTime.Parse(response.Data.ExpiryDate!),
                    response.Data.Status!);
            }
            // Handle invalid license (400 Bad Request)
            else if (statusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning(
                    "[{RequestId}] License key validation failed: {Message}",
                    requestId, response?.Message);

                _telemetryService?.TrackDependency(
                    "HTTP",
                    "LicensingService",
                    "ValidateLicenseKey",
                    duration,
                    false,
                    400);

                return LicenseKeyValidationResult.Failure(
                    response?.Message ?? "Invalid license key",
                    400);
            }
            // Handle unexpected responses
            else
            {
                _logger.LogError(
                    "[{RequestId}] License key validation returned unexpected status: {StatusCode}",
                    requestId, statusCode);

                _telemetryService?.TrackDependency(
                    "HTTP",
                    "LicensingService",
                    "ValidateLicenseKey",
                    duration,
                    false,
                    (int)statusCode);

                if (failOpen)
                {
                    return LicenseKeyValidationResult.FailOpen($"Unexpected status {statusCode}");
                }

                return LicenseKeyValidationResult.Failure(
                    $"License validation service returned {statusCode}",
                    (int)statusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex,
                "[{RequestId}] HTTP request failed during license key validation",
                requestId);

            _telemetryService?.TrackDependency(
                "HTTP",
                "LicensingService",
                "ValidateLicenseKey",
                duration,
                false,
                null,
                ex.Message);

            if (failOpen)
            {
                _logger.LogWarning("[{RequestId}] FAIL-OPEN: Allowing operation due to network error", requestId);
                return LicenseKeyValidationResult.FailOpen($"Network error: {ex.Message}");
            }

            return LicenseKeyValidationResult.Failure($"Network error: {ex.Message}", 503);
        }
        catch (TaskCanceledException ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex,
                "[{RequestId}] Request timeout during license key validation",
                requestId);

            _telemetryService?.TrackDependency(
                "HTTP",
                "LicensingService",
                "ValidateLicenseKey",
                duration,
                false,
                408,
                "Request timeout");

            if (failOpen)
            {
                _logger.LogWarning("[{RequestId}] FAIL-OPEN: Allowing operation due to timeout", requestId);
                return LicenseKeyValidationResult.FailOpen("Request timeout");
            }

            return LicenseKeyValidationResult.Failure("Request timeout", 408);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex,
                "[{RequestId}] Unexpected error during license key validation",
                requestId);

            _telemetryService?.TrackDependency(
                "HTTP",
                "LicensingService",
                "ValidateLicenseKey",
                duration,
                false,
                500,
                ex.Message);

            if (failOpen)
            {
                _logger.LogWarning("[{RequestId}] FAIL-OPEN: Allowing operation due to unexpected error", requestId);
                return LicenseKeyValidationResult.FailOpen($"Unexpected error: {ex.Message}");
            }

            return LicenseKeyValidationResult.Failure($"Unexpected error: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Generic POST method for licensing service calls
    /// </summary>
    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : class, new()
    {
        var requestId = Guid.NewGuid().ToString();


        try
        {
            // Build the proper relative endpoint (remove leading "/" to avoid path replacement)
            var relativeEndpoint = BuildEndpoint(endpoint);
            
            if (_options.EnableRequestLogging)
            {
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                _logger.LogInformation(
                    "[{RequestId}] Licensing Service POST {BaseAddress}{Endpoint}: {Request}",
                    requestId, _httpClient.BaseAddress, relativeEndpoint, requestJson);
            }

            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, relativeEndpoint)
            {
                Content = content
            };

            // Add authorization header from configuration
            requestMessage.Headers.Add("Authorize", _options.AuthorizationKey);

            var httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (_options.EnableResponseLogging)
            {
                _logger.LogInformation(
                    "[{RequestId}] Licensing Service Response ({StatusCode}): {Response}",
                    requestId, httpResponse.StatusCode, responseBody);
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[{RequestId}] Licensing Service error: {StatusCode} - {Response}",
                    requestId, httpResponse.StatusCode, responseBody);

                // Return error response with status code
                var errorResponse = new TResponse();
                if (errorResponse is GenerateLicenseResponse genResponse)
                {
                    genResponse.Status = (int)httpResponse.StatusCode;
                    genResponse.Message = $"Request failed: {responseBody}";
                }
                return errorResponse;
            }

            var response = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);

            if (response == null)
            {
                throw new InvalidOperationException("Failed to deserialize response");
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] HTTP request failed for {Endpoint}",
                requestId, endpoint);

            var errorResponse = new TResponse();
            if (errorResponse is GenerateLicenseResponse genResponse)
            {
                genResponse.Status = 500;
                genResponse.Message = $"HTTP request failed: {ex.Message}";
            }
            return errorResponse;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] Request timeout for {Endpoint}",
                requestId, endpoint);

            var errorResponse = new TResponse();
            if (errorResponse is GenerateLicenseResponse genResponse)
            {
                genResponse.Status = 408;
                genResponse.Message = "Request timeout";
            }
            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] Unexpected error for {Endpoint}",
                requestId, endpoint);

            var errorResponse = new TResponse();
            if (errorResponse is GenerateLicenseResponse genResponse)
            {
                genResponse.Status = 500;
                genResponse.Message = $"Unexpected error: {ex.Message}";
            }
            return errorResponse;
        }
    }

    /// <summary>
    /// Generic GET method for licensing service calls
    /// Returns response and status code
    /// </summary>
    private async Task<(TResponse? Response, HttpStatusCode StatusCode)> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var requestId = Guid.NewGuid().ToString();

        try
        {
            // Build the proper relative endpoint (remove leading "/" to avoid path replacement)
            var relativeEndpoint = BuildEndpoint(endpoint);
            
            if (_options.EnableRequestLogging)
            {
                _logger.LogInformation(
                    "[{RequestId}] Licensing Service GET {BaseAddress}{Endpoint}",
                    requestId, _httpClient.BaseAddress, relativeEndpoint);
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, relativeEndpoint);
            
            // Add authorization header from configuration
            requestMessage.Headers.Add("Authorize", _options.AuthorizationKey);

            var httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (_options.EnableResponseLogging)
            {
                _logger.LogInformation(
                    "[{RequestId}] Licensing Service Response ({StatusCode}): {Response}",
                    requestId, httpResponse.StatusCode, responseBody);
            }

            // Return both response and status code for caller to handle
            if (!httpResponse.IsSuccessStatusCode)
            {
                // Try to parse error response
                TResponse? errorResponse = null;
                try
                {
                    errorResponse = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);
                }
                catch
                {
                    // Ignore deserialization errors for error responses
                }

                return (errorResponse, httpResponse.StatusCode);
            }

            var response = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);
            return (response, httpResponse.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] HTTP request failed for {Endpoint}",
                requestId, endpoint);

            return (null, HttpStatusCode.ServiceUnavailable);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] Request timeout for {Endpoint}",
                requestId, endpoint);

            return (null, HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] Unexpected error for {Endpoint}",
                requestId, endpoint);

            return (null, HttpStatusCode.InternalServerError);
        }
    }
}