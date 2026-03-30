using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.DownloadInvoice;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.FIRSAccessPoint.Services;

public sealed partial class FIRSHttpClient : IFIRSHttpClient
{
    public async Task<DownloadInvoiceResponse> DownloadInvoiceAsync(string irn, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);

        _logger.LogInformation("Downloading invoice: {irn}", irn);

        var endpoint = BuildEndpoint(_options.DownloadInvoiceEndpoint, irn);
        return await _integrationService.GetDataAsync<DownloadInvoiceResponse>(endpoint, apiKey, apiSecret, cancellationToken);
    }

    public async Task<bool> ValidateConnectionAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating FIRS connection");

        return await _integrationService.ValidateConnectionAsync(apiKey, apiSecret, cancellationToken);
    }

    private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        string endpoint,
        string apiKey,
        string apiSecret,
        HttpMethod httpMethod,
        CancellationToken cancellationToken)
        where TRequest : notnull
        where TResponse : notnull
    {
        var requestJson = JsonSerializer.Serialize(request);
        var responseJson = await _integrationService.SendDataAsync(httpMethod, endpoint,requestJson, apiKey, apiSecret, cancellationToken);

        var response = JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        return response ?? throw new InvalidOperationException($"Response deserialization failed for {typeof(TResponse).Name}");
    }

    private string BuildEndpoint(string template, params string[] parameters)
    {
        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/{template.TrimStart('/')}";

        if (parameters.Length > 0)
        {
            endpoint = string.Format(endpoint, parameters);
        }

        return endpoint;
    }
}
