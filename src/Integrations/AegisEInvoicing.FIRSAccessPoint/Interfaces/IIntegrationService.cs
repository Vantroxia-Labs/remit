namespace AegisEInvoicing.FIRSAccessPoint.Interfaces;

public interface IIntegrationService
{
    Task<string> SendDataAsync(HttpMethod httpMethod, string url, string data, string apiKey, string apiSecret, CancellationToken cancellationToken = default);
    Task<T> GetDataAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<T> GetDataAsync<T>(string endpoint, string apiKey, string apiSecret, CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default);
}