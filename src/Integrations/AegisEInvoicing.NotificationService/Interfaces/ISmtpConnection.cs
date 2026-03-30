using MimeKit;

namespace AegisEInvoicing.NotificationService.Interfaces;

public interface ISmtpConnection : IDisposable
{
    string ConnectionId { get; }
    bool IsConnected { get; }
    bool IsAuthenticated { get; }
    DateTime LastUsed { get; }
    DateTime CreatedAt { get; }
    Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default);
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task AuthenticateAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
