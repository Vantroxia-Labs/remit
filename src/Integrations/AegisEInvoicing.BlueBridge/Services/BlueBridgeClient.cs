using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AegisEInvoicing.BlueBridge.Configuration;
using AegisEInvoicing.BlueBridge.Contracts;
using AegisEInvoicing.BlueBridge.Converters;
using AegisEInvoicing.BlueBridge.Exceptions;
using AegisEInvoicing.BlueBridge.Models.Requests;
using AegisEInvoicing.BlueBridge.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.BlueBridge.Services;

/// <summary>
/// HTTP client implementation for BlueBridge e-invoice integration.
/// All requests are authenticated via the <c>X-API-Key</c> header.
/// </summary>
internal sealed class BlueBridgeClient : IBlueBridgeClient
{
    private readonly HttpClient _httpClient;
    private readonly BlueBridgeOptions _options;
    private readonly ILogger<BlueBridgeClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Dynamic overrides from Access Point Provider (database)
    private string? _apiKeyOverride;
    private string _effectiveBaseUrl;

    public BlueBridgeClient(
        HttpClient httpClient,
        IOptions<BlueBridgeOptions> options,
        ILogger<BlueBridgeClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        _jsonOptions.Converters.Add(new DateOnlyJsonConverter());
        _jsonOptions.Converters.Add(new TimeOnlyJsonConverter());

        _effectiveBaseUrl = _options.BaseUrl ?? string.Empty;
    }

    public void Configure(string baseUrl, string apiKey)
    {
        _apiKeyOverride = apiKey;

        if (!string.IsNullOrWhiteSpace(baseUrl))
            _effectiveBaseUrl = baseUrl;

        _logger.LogInformation("BlueBridgeClient configured with credentials from Access Point Provider");
    }

    private string EffectiveApiKey => _apiKeyOverride ?? _options.ApiKey;

    #region Invoice Operations

    public async Task<GenerateIrnResponse> GenerateIrnAsync(
        string reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        var endpoint = $"{_options.GenerateIrnEndpoint}?reference={Uri.EscapeDataString(reference)}";
        return await GetAsync<GenerateIrnResponse>(endpoint, cancellationToken);
    }

    public async Task<ValidateIrnResponse> ValidateIrnAsync(
        ValidateIrnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<ValidateIrnRequest, ValidateIrnResponse>(
            _options.ValidateIrnEndpoint, request, cancellationToken);
    }

    public async Task<ValidateInvoiceResponse> ValidateInvoiceAsync(
        BlueBridgeInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<BlueBridgeInvoiceRequest, ValidateInvoiceResponse>(
            _options.ValidateInvoiceEndpoint, request, cancellationToken);
    }

    public async Task<SignInvoiceResponse> SignInvoiceAsync(
        BlueBridgeInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<BlueBridgeInvoiceRequest, SignInvoiceResponse>(
            _options.SignInvoiceEndpoint, request, cancellationToken);
    }

    public async Task<TransmitInvoiceResponse> TransmitInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);
        return await PostEmptyAsync<TransmitInvoiceResponse>(
            $"{_options.TransmitInvoiceEndpoint}/{Uri.EscapeDataString(irn)}", cancellationToken);
    }

    public async Task<LookupWithTinResponse> LookupWithTinAsync(
        string tin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tin);
        return await GetAsync<LookupWithTinResponse>(
            $"{_options.LookupWithTinEndpoint}/{Uri.EscapeDataString(tin)}", cancellationToken);
    }

    public async Task<LookupWithIrnResponse> LookupWithIrnAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);
        return await GetAsync<LookupWithIrnResponse>(
            $"{_options.LookupWithIrnEndpoint}/{Uri.EscapeDataString(irn)}", cancellationToken);
    }

    public async Task<UpdateInvoiceResponse> UpdateInvoiceAsync(
        string irn,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);
        ArgumentNullException.ThrowIfNull(request);
        return await PatchAsync<UpdateInvoiceRequest, UpdateInvoiceResponse>(
            $"{_options.UpdateInvoiceEndpoint}/{Uri.EscapeDataString(irn)}", request, cancellationToken);
    }

    public async Task<SearchInvoicesResponse> SearchInvoicesAsync(
        string businessId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(businessId);
        return await GetAsync<SearchInvoicesResponse>(
            $"{_options.SearchInvoicesEndpoint}/{Uri.EscapeDataString(businessId)}", cancellationToken);
    }

    public async Task<HealthCheckResponse> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<HealthCheckResponse>(_options.HealthEndpoint, cancellationToken);
    }

    public async Task<ConfirmInvoiceResponse> ConfirmInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);
        return await GetAsync<ConfirmInvoiceResponse>(
            $"{_options.ConfirmInvoiceEndpoint}/{Uri.EscapeDataString(irn)}", cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HealthCheckAsync(cancellationToken);
            return response.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BlueBridge connection test failed");
            return false;
        }
    }

    #endregion

    #region Private HTTP Helpers

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var body = JsonSerializer.Serialize(request, _jsonOptions);
        return await SendAsync<TResponse>(HttpMethod.Post, endpoint, body, cancellationToken);
    }

    private async Task<TResponse> PostEmptyAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        return await SendAsync<TResponse>(HttpMethod.Post, endpoint, body: null, cancellationToken);
    }

    private async Task<TResponse> PatchAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var body = JsonSerializer.Serialize(request, _jsonOptions);
        return await SendAsync<TResponse>(HttpMethod.Patch, endpoint, body, cancellationToken);
    }

    private async Task<TResponse> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        return await SendAsync<TResponse>(HttpMethod.Get, endpoint, body: null, cancellationToken);
    }

    private async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method,
        string endpoint,
        string? body,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var requestId = Guid.NewGuid().ToString();

        try
        {
            if (_options.EnableRequestLogging && body is not null)
            {
                _logger.LogInformation(
                    "[{RequestId}] BlueBridge API Request {Method} {Endpoint}: {Body}",
                    requestId, method, endpoint, body);
            }

            var requestUri = !string.IsNullOrWhiteSpace(_effectiveBaseUrl)
                ? _effectiveBaseUrl.TrimEnd('/') + "/" + endpoint.TrimStart('/')
                : endpoint;

            using var requestMessage = new HttpRequestMessage(method, requestUri);

            if (body is not null)
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");

            requestMessage.Headers.TryAddWithoutValidation("X-API-Key", EffectiveApiKey);

            var httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (_options.EnableResponseLogging)
            {
                _logger.LogInformation(
                    "[{RequestId}] BlueBridge API Response from {Endpoint} ({StatusCode}): {Response}",
                    requestId, endpoint, httpResponse.StatusCode, responseBody);
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[{RequestId}] BlueBridge API error from {Endpoint}: {StatusCode} - {Response}",
                    requestId, endpoint, httpResponse.StatusCode, responseBody);

                throw new BlueBridgeIntegrationException(
                    $"BlueBridge API request failed with status {httpResponse.StatusCode}",
                    (int)httpResponse.StatusCode,
                    responseBody);
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new BlueBridgeIntegrationException(
                    $"BlueBridge API returned empty response for endpoint {endpoint}",
                    (int)httpResponse.StatusCode,
                    "Empty response body");
            }

            var response = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);

            return response ?? throw new BlueBridgeIntegrationException(
                "Failed to deserialize BlueBridge API response",
                500,
                responseBody);
        }
        catch (BlueBridgeIntegrationException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "[{RequestId}] BlueBridge API request to {Endpoint} timed out", requestId, endpoint);
            throw new BlueBridgeIntegrationException($"BlueBridge API request to {endpoint} timed out", 408);
        }
        catch (Exception ex) when (ex is not BlueBridgeIntegrationException)
        {
            _logger.LogError(ex, "[{RequestId}] Unexpected error during BlueBridge API call to {Endpoint}", requestId, endpoint);
            throw new BlueBridgeIntegrationException($"Unexpected error during BlueBridge API call: {ex.Message}", ex);
        }
    }

    #endregion
}
