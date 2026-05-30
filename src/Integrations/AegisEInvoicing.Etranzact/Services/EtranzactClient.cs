using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AegisEInvoicing.Etranzact.Configuration;
using AegisEInvoicing.Etranzact.Contracts;
using AegisEInvoicing.Etranzact.Converters;
using AegisEInvoicing.Etranzact.Exceptions;
using AegisEInvoicing.Etranzact.Models.Requests;
using AegisEInvoicing.Etranzact.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.Etranzact.Services;

/// <summary>
/// HTTP client implementation for eTranzact e-invoice integration.
/// Authentication uses per-request HMAC-SHA256 signed headers instead of OAuth2 tokens.
/// Signature = Base64( HMAC-SHA256( CLIENT_SECRET_KEY, requestBody + timestamp ) )
/// </summary>
internal sealed class EtranzactClient : IEtranzactClient
{
    private readonly HttpClient _httpClient;
    private readonly EtranzactOptions _options;
    private readonly ILogger<EtranzactClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Dynamic overrides from Access Point Provider (database)
    private string? _clientApiKeyOverride;
    private string? _clientSecretKeyOverride;

    public EtranzactClient(
        HttpClient httpClient,
        IOptions<EtranzactOptions> options,
        ILogger<EtranzactClient> logger)
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
    }

    
    public void Configure(string baseUrl, string clientApiKey, string clientSecretKey)
    {
        _clientApiKeyOverride = clientApiKey;
        _clientSecretKeyOverride = clientSecretKey;

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        _logger.LogInformation("EtranzactClient configured with credentials from Access Point Provider");
    }

    private string EffectiveClientApiKey => _clientApiKeyOverride ?? _options.ClientApiKey;
    private string EffectiveClientSecretKey => _clientSecretKeyOverride ?? _options.ClientSecretKey;

    #region Invoice Operations

    public async Task<ValidateInvoiceResponse> ValidateInvoiceAsync(
        ValidateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<ValidateInvoiceRequest, ValidateInvoiceResponse>(
            _options.ValidateInvoiceEndpoint, request, cancellationToken);
    }

    
    public async Task<SignInvoiceResponse> SignInvoiceAsync(
        SignInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<SignInvoiceRequest, SignInvoiceResponse>(
            _options.SignInvoiceEndpoint, request, cancellationToken);
    }

    
    public async Task<TransmitInvoiceResponse> TransmitInvoiceAsync(
        TransmitInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<TransmitInvoiceRequest, TransmitInvoiceResponse>(
            _options.TransmitInvoiceEndpoint, request, cancellationToken);
    }

    
    public async Task<ConfirmInvoiceResponse> ConfirmInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);
        return await GetAsync<ConfirmInvoiceResponse>(
            $"{_options.ConfirmInvoiceEndpoint}/{irn}", cancellationToken);
    }

    
    public async Task<UpdatePaymentStatusResponse> UpdatePaymentStatusAsync(
        string irn,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);
        ArgumentNullException.ThrowIfNull(request);
        return await PatchAsync<UpdatePaymentStatusRequest, UpdatePaymentStatusResponse>(
            $"{_options.UpdateInvoiceEndpoint}/{irn}", request, cancellationToken);
    }

    
    public async Task<VerifyTinResponse> VerifyTinAsync(
        VerifyTinRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<VerifyTinRequest, VerifyTinResponse>(
            _options.VerifyTinEndpoint, request, cancellationToken);
    }

    
    public async Task<ValidateIrnResponse> ValidateIrnAsync(
        ValidateIrnRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<ValidateIrnRequest, ValidateIrnResponse>(
            _options.ValidateIrnEndpoint, request, cancellationToken);
    }

    
    public Task<NotImplementedException> DownloadInvoiceAsync(CancellationToken cancellationToken = default)
        => Task.FromException<NotImplementedException>(
            new NotImplementedException("DownloadInvoice is not yet available on the eTranzact API."));

    
    public Task<NotImplementedException> SearchInvoiceAsync(CancellationToken cancellationToken = default)
        => Task.FromException<NotImplementedException>(
            new NotImplementedException("SearchInvoice is not yet available on the eTranzact API."));

    
    public Task<NotImplementedException> GetEntityAsync(CancellationToken cancellationToken = default)
        => Task.FromException<NotImplementedException>(
            new NotImplementedException("GetEntity is not yet available on the eTranzact API."));

    
    public Task<NotImplementedException> GetPurchaseInvoicesAsync(CancellationToken cancellationToken = default)
        => Task.FromException<NotImplementedException>(
            new NotImplementedException("GetPurchaseInvoices is not yet available on the eTranzact API."));

    
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(string.Empty, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test to eTranzact API failed");
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
            var timestamp = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var signature = ComputeSignature(body ?? string.Empty, timestamp);

            if (_options.EnableRequestLogging && body is not null)
            {
                _logger.LogInformation(
                    "[{RequestId}] eTranzact API Request {Method} {Endpoint}: {Body}",
                    requestId, method, endpoint, body);
            }

            using var requestMessage = new HttpRequestMessage(method, endpoint);

            if (body is not null)
            {
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            requestMessage.Headers.TryAddWithoutValidation("X-API-Key", EffectiveClientApiKey);
            requestMessage.Headers.TryAddWithoutValidation("X-API-Signature", signature);
            requestMessage.Headers.TryAddWithoutValidation("X-API-Timestamp", timestamp);

            var httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            if (_options.EnableResponseLogging)
            {
                _logger.LogInformation(
                    "[{RequestId}] eTranzact API Response from {Endpoint} ({StatusCode}): {Response}",
                    requestId, endpoint, httpResponse.StatusCode, responseBody);
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[{RequestId}] eTranzact API error from {Endpoint}: {StatusCode} - {Response}",
                    requestId, endpoint, httpResponse.StatusCode, responseBody);

                throw new EtranzactIntegrationException(
                    $"eTranzact API request failed with status {httpResponse.StatusCode}",
                    (int)httpResponse.StatusCode,
                    responseBody);
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new EtranzactIntegrationException(
                    $"eTranzact API returned empty response for endpoint {endpoint}",
                    (int)httpResponse.StatusCode,
                    "Empty response body");
            }

            var response = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);

            return response is null
                ? throw new EtranzactIntegrationException(
                    "Failed to deserialize eTranzact API response",
                    500,
                    responseBody)
                : response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] HTTP request exception calling eTranzact API endpoint {Endpoint}",
                requestId, endpoint);
            throw new EtranzactIntegrationException(
                $"HTTP request failed for endpoint {endpoint}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] Request timeout calling eTranzact API endpoint {Endpoint}",
                requestId, endpoint);
            throw new EtranzactIntegrationException(
                $"Request timeout for endpoint {endpoint}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] JSON deserialization error for eTranzact API endpoint {Endpoint}",
                requestId, endpoint);
            throw new EtranzactIntegrationException(
                $"Failed to parse response from endpoint {endpoint}: {ex.Message}", ex);
        }
    }

    #endregion

    #region HMAC Signature

    /// <summary>
    /// Computes the HMAC-SHA256 signature for a request.
    /// Signature = Base64( HMAC-SHA256( CLIENT_SECRET_KEY, requestBody + timestamp ) )
    /// </summary>
    private string ComputeSignature(string requestBody, string timestamp)
    {
        var message = requestBody + timestamp;
        var keyBytes = Encoding.UTF8.GetBytes(EffectiveClientSecretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}
