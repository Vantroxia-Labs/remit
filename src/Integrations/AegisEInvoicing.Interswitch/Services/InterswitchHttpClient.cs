using AegisEInvoicing.Interswitch.Configuration;
using AegisEInvoicing.Interswitch.Converters;
using AegisEInvoicing.Interswitch.Exceptions;
using AegisEInvoicing.Interswitch.Interfaces;
using AegisEInvoicing.Interswitch.Models.Requests.ConfirmInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.DownloadInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.GetEntity;
using AegisEInvoicing.Interswitch.Models.Requests.GetPurchaseInvoices;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithIRN;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithTIN;
using AegisEInvoicing.Interswitch.Models.Requests.SearchInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.SignInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.Token;
using AegisEInvoicing.Interswitch.Models.Requests.TransmitInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.UpdateStatus;
using AegisEInvoicing.Interswitch.Models.Requests.ValidateInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.ValidateIRN;
using AegisEInvoicing.Interswitch.Models.Responses.ConfirmInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.DownloadInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.GetEntity;
using AegisEInvoicing.Interswitch.Models.Responses.GetPurchaseInvoices;
using AegisEInvoicing.Interswitch.Models.Responses.LookupWithIRN;
using AegisEInvoicing.Interswitch.Models.Responses.LookupWithTIN;
using AegisEInvoicing.Interswitch.Models.Responses.SearchInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.SignInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.Token;
using AegisEInvoicing.Interswitch.Models.Responses.TransmitInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.UpdateStatus;
using AegisEInvoicing.Interswitch.Models.Responses.ValidateInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.ValidateIRN;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AegisEInvoicing.Interswitch.Services;

/// <summary>
/// HTTP client implementation for Interswitch SwitchTax integration
/// </summary>
public sealed class InterswitchHttpClient : IInterswitchHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly InterswitchHttpClientOptions _options;
    private readonly ILogger<InterswitchHttpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTime _tokenExpiration = DateTime.MinValue;

    public InterswitchHttpClient(
        HttpClient httpClient,
        IOptions<InterswitchHttpClientOptions> options,
        ILogger<InterswitchHttpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // Add converter for handling stringified JSON in data fields
        _jsonOptions.Converters.Add(new InterswitchResponseConverterFactory());

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = _options.RequestTimeout;

        // Add default headers
        foreach (var header in _options.DefaultHeaders)
        {
            if (!_httpClient.DefaultRequestHeaders.Contains(header.Key))
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// Ensures that a valid access token is available for API requests
    /// </summary>
    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        // If we have a valid token, return immediately
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
        {
            return;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return;
            }

            _logger.LogInformation("Requesting new access token from Interswitch");

            var tokenRequest = new TokenRequest
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret
            };

            var content = new StringContent(
                JsonSerializer.Serialize(tokenRequest, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_options.TokenEndpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Token endpoint response: Status={StatusCode}, Body={ResponseBody}",
                response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to obtain access token from Interswitch: {StatusCode} - {Response}",
                    response.StatusCode, responseBody);

                throw new InterswitchIntegrationException(
                    $"Authentication failed with status {response.StatusCode}",
                    (int)response.StatusCode,
                    responseBody);
            }

            // Check for empty response body
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                _logger.LogError(
                    "Empty response body from Interswitch token endpoint. BaseUrl={BaseUrl}, TokenEndpoint={TokenEndpoint}",
                    _options.BaseUrl, _options.TokenEndpoint);

                throw new InterswitchIntegrationException(
                    $"Interswitch token endpoint returned empty response. URL: {_options.BaseUrl}{_options.TokenEndpoint}",
                    (int)response.StatusCode,
                    "Empty response body from token endpoint");
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody, _jsonOptions);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InterswitchIntegrationException(
                    "Failed to deserialize token response or access token is empty",
                    500,
                    responseBody);
            }

            _accessToken = tokenResponse.AccessToken;
            // Set expiration to 5 minutes before actual expiration to avoid edge cases
            _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);

            _logger.LogInformation("Successfully obtained access token, expires at {Expiration}", _tokenExpiration);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<ValidateIRNResponse> ValidateIRNAsync(
        ValidateIRNRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<ValidateIRNRequest, ValidateIRNResponse>(
            _options.ValidateIRNEndpoint,
            request,
            cancellationToken);
    }

    public async Task<ValidateInvoiceResponse> ValidateInvoiceAsync(
        ValidateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<ValidateInvoiceRequest, ValidateInvoiceResponse>(
            _options.ValidateInvoiceEndpoint,
            request,
            cancellationToken);
    }

    public async Task<ConfirmInvoiceWrappedResponse> ConfirmInvoiceAsync(
       ConfirmInvoiceRequest request,
       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<ConfirmInvoiceRequest, ConfirmInvoiceWrappedResponse>(
            _options.ConfirmInvoiceEndpoint,
            request,
            cancellationToken);
    }

    public async Task<SignInvoiceResponse> SignInvoiceAsync(
        SignInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<SignInvoiceRequest, SignInvoiceResponse>(
            _options.SignInvoiceEndpoint,
            request,
            cancellationToken);
    }

    public async Task<UpdateStatusResponse> UpdateStatusAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<UpdateStatusRequest, UpdateStatusResponse>(
            _options.UpdateStatusEndpoint,
            request,
            cancellationToken);
    }

    public async Task<DownloadInvoiceResponse> DownloadInvoiceAsync(
        DownloadInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<DownloadInvoiceRequest, DownloadInvoiceResponse>(
            _options.DownloadInvoiceEndpoint,
            request,
            cancellationToken);
    }

    public async Task<SearchInvoiceResponse> SearchInvoiceAsync(
        SearchInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<SearchInvoiceRequest, SearchInvoiceResponse>(
            _options.SearchInvoiceEndpoint,
            request,
            cancellationToken);
    }

    public async Task<LookupWithIRNResponse> LookupWithIRNAsync(
        LookupWithIRNRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<LookupWithIRNRequest, LookupWithIRNResponse>(
            _options.LookupWithIRNEndpoint,
            request,
            cancellationToken);
    }

    public async Task<TransmitInvoiceResponse> TransmitInvoiceAsync(
        TransmitInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<TransmitInvoiceRequest, TransmitInvoiceResponse>(
            _options.TransmitInvoiceEndpoint,
            request,
            cancellationToken);
    }

    public async Task<LookupWithTINResponse> LookupWithTINAsync(
        LookupWithTINRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<LookupWithTINRequest, LookupWithTINResponse>(
            _options.LookupWithTINEndpoint,
            request,
            cancellationToken);
    }

    public async Task<GetEntityResponse> GetEntityAsync(
        GetEntityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<GetEntityRequest, GetEntityResponse>(
            _options.GetEntityEndpoint,
            request,
            cancellationToken);
    }

    public async Task<GetPurchaseInvoicesResponse> GetPurchaseInvoicesAsync(
        GetPurchaseInvoicesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<GetPurchaseInvoicesRequest, GetPurchaseInvoicesResponse>(
            _options.GetPurchaseInvoicesEndpoint,
            request,
            cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(string.Empty, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test to Interswitch API failed");
            return false;
        }
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var requestId = Guid.NewGuid().ToString();

        try
        {
            // Ensure we have a valid access token
            await EnsureAuthenticatedAsync(cancellationToken);

            if (_options.EnableRequestLogging)
            {
                var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                _logger.LogInformation(
                    "[{RequestId}] Interswitch API Request to {Endpoint}: {Request}",
                    requestId, endpoint, requestJson);
            }


            var data = JsonSerializer.Serialize(request, _jsonOptions);
            var sanitizedData = await ValidateAndSanitizeDataAsync(data, cancellationToken);

            var content = new StringContent(
                sanitizedData,
                Encoding.UTF8,
                "application/json");

            // Create request message to add authorization header
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };

            // Add bearer token to request
            if (!string.IsNullOrEmpty(_accessToken))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            }

            var httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);

            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);            

            if (_options.EnableResponseLogging)
            {
                _logger.LogInformation(
                    "[{RequestId}] Interswitch API Response from {Endpoint} ({StatusCode}): {Response}",
                    requestId, endpoint, httpResponse.StatusCode, responseBody);
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[{RequestId}] Interswitch API error response from {Endpoint}: {StatusCode} - {Response}",
                    requestId, endpoint, httpResponse.StatusCode, responseBody);

                throw new InterswitchIntegrationException(
                    $"Interswitch API request failed with status {httpResponse.StatusCode}",
                    (int)httpResponse.StatusCode,
                    responseBody);
            }

            // Check for empty response body
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                _logger.LogError(
                    "[{RequestId}] Empty response body from Interswitch API endpoint {Endpoint}",
                    requestId, endpoint);

                throw new InterswitchIntegrationException(
                    $"Interswitch API returned empty response for endpoint {endpoint}",
                    (int)httpResponse.StatusCode,
                    "Empty response body");
            }

            var response = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);

            if (response == null)
            {
                throw new InterswitchIntegrationException(
                    "Failed to deserialize Interswitch API response",
                    500,
                    responseBody);
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] HTTP request exception calling Interswitch API endpoint {Endpoint}",
                requestId, endpoint);
            throw new InterswitchIntegrationException(
                $"HTTP request failed for endpoint {endpoint}: {ex.Message}",
                ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] Request timeout calling Interswitch API endpoint {Endpoint}",
                requestId, endpoint);
            throw new InterswitchIntegrationException(
                $"Request timeout for endpoint {endpoint}",
                ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "[{RequestId}] JSON deserialization error for Interswitch API endpoint {Endpoint}",
                requestId, endpoint);
            throw new InterswitchIntegrationException(
                $"Failed to parse response from endpoint {endpoint}: {ex.Message}",
                ex);
        }
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
}
