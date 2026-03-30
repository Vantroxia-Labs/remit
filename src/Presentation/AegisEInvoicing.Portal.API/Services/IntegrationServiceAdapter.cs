using Microsoft.AspNetCore.DataProtection;

namespace AegisEInvoicing.Portal.API.Services;

public sealed class IntegrationServiceAdapter(Application.Common.Interfaces.IIntegrationService integrationService) : FIRSAccessPoint.Interfaces.IIntegrationService
{
    private readonly Application.Common.Interfaces.IIntegrationService _integrationService = integrationService ?? throw new ArgumentNullException(nameof(integrationService));

    public Task<string> SendDataAsync(HttpMethod httpMethod, string url, string data, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        return _integrationService.SendDataAsync(httpMethod, url, data, apiKey, apiSecret, cancellationToken);
    }

    public Task<T> GetDataAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        return _integrationService.GetDataAsync<T>(endpoint, cancellationToken);
    }

    public Task<T> GetDataAsync<T>(string endpoint, string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        return _integrationService.GetDataAsync<T>(endpoint, apiKey, apiSecret, cancellationToken);
    }    

    public Task<bool> ValidateConnectionAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default)
    {
        return _integrationService.ValidateConnectionAsync(apiKey, apiSecret, cancellationToken);
    }
}