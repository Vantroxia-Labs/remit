namespace AegisEInvoicing.NotificationService.Interfaces;

public interface ISmtpConnectionManager : IDisposable
{
    Task<ISmtpConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    Task ReturnConnectionAsync(ISmtpConnection connection, bool isHealthy = true);
    bool ValidateConnectionHealth(ISmtpConnection connection);
}
