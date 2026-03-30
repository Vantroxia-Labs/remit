using AegisEInvoicing.FIRSAccessPoint.Attributes;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.Authentication;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ReportInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.SignInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.UpdateInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateIRN;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.Authentication;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ConfirmInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ReportInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.SignInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.UpdateInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ValidateInvoiceData;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ValidateIRN;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AegisEInvoicing.FIRSAccessPoint.Services;

/// <summary>
/// HTTP client implementation for FIRS integration.
/// This service is tenant-agnostic and provides shared functionality across all tenants.
/// </summary>
[TenantAgnostic("FIRS integration operates as a shared service independent of tenant context")]
public sealed partial class FIRSHttpClient : IFIRSHttpClient
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<FIRSHttpClient> _logger;
    private readonly FIRSHttpClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public FIRSHttpClient(
        IIntegrationService integrationService,
        ILogger<FIRSHttpClient> logger,
        IOptions<FIRSHttpClientOptions> options)
    {
        _integrationService = integrationService ?? throw new ArgumentNullException(nameof(integrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Initiating FIRS authentication for email: {Email}", request.Email);

        var endpoint = BuildEndpoint(_options.AuthenticationEndpoint);
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        var responseJson = await _integrationService.SendDataAsync(HttpMethod.Post, endpoint, requestJson, apiKey, apiSecret, cancellationToken);

        var response = JsonSerializer.Deserialize<AuthenticationResponse>(responseJson, _jsonOptions);
        
        if (response?.Data != null)
        {
            _logger.LogInformation("Authentication successful for email: {Email}, EntityId: {EntityId}", 
                request.Email, response.Data.EntityId);
        }

        return response ?? throw new InvalidOperationException("Authentication response is null");
    }

    public async Task<ValidateInvoiceDataResponse> ValidateInvoiceDataAsync(ValidateInvoiceDataRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Validating invoice data");

        var endpoint = BuildEndpoint(_options.ValidateInvoiceDataEndpoint);
        return await SendRequestAsync<ValidateInvoiceDataRequest, ValidateInvoiceDataResponse>(
            request, endpoint, apiKey, apiSecret, HttpMethod.Post, cancellationToken);
    }

    public async Task<ValidateIrnResponse> ValidateIrnAsync(ValidateIrnRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Validating IRN");

        var endpoint = BuildEndpoint(_options.ValidateIrnEndpoint);
        return await SendRequestAsync<ValidateIrnRequest, ValidateIrnResponse>(
            request, endpoint, apiKey, apiSecret, HttpMethod.Post, cancellationToken);
    }

    public async Task<SignInvoiceResponse> SignInvoiceAsync(SignInvoiceRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Signing invoice");

        var endpoint = BuildEndpoint(_options.SignInvoiceEndpoint);
        return await SendRequestAsync<SignInvoiceRequest, SignInvoiceResponse>(
            request, endpoint, apiKey, apiSecret, HttpMethod.Post, cancellationToken);
    }

    public async Task<ReportInvoiceResponse> ReportInvoiceAsync(ReportInvoiceRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Reporting invoice to FIRS");

        var endpoint = BuildEndpoint(_options.ReportInvoiceEndpoint);
        return await SendRequestAsync<ReportInvoiceRequest, ReportInvoiceResponse>(
            request, endpoint, apiKey, apiSecret, HttpMethod.Post, cancellationToken);
    }

    public async Task<UpdateInvoiceResponse> UpdateInvoiceAsync(string irn, UpdateInvoiceRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Updating invoice");

        var endpoint = BuildEndpoint(_options.UpdateInvoiceEndpoint, irn);
        return await SendRequestAsync<UpdateInvoiceRequest, UpdateInvoiceResponse>(
            request, endpoint, apiKey, apiSecret, HttpMethod.Patch, cancellationToken);
    }

    public async Task<ConfirmInvoiceResponse> ConfirmInvoiceAsync(string irn, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);

        _logger.LogInformation("Confirming invoice: {irn}", irn);

        var endpoint = BuildEndpoint(_options.ConfirmInvoiceEndpoint, irn);
        return await _integrationService.GetDataAsync<ConfirmInvoiceResponse>(endpoint, apiKey, apiSecret, cancellationToken);
    }    
}