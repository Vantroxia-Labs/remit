namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for integrating with external systems
/// </summary>
public interface IIntegrationService
{
    Task<string> SendDataAsync(HttpMethod httpMethod, string url, string data, string apiKey, string apiSecret, CancellationToken cancellationToken = default);
    Task<T> GetDataAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<T> GetDataAsync<T>(string endpoint, string apiKey, string apiSecret, CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default);
}