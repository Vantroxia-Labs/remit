using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AegisEInvoicing.NotificationService.Configurations;

public class MailKitSmtpConnection : ISmtpConnection
{
    private readonly SmtpClient _client;
    private readonly MailKitConfiguration _config;
    private readonly ILogger<MailKitSmtpConnection> _logger;
    private bool _disposed;

    public string ConnectionId { get; } = Guid.NewGuid().ToString();
    public bool IsConnected => _client.IsConnected;
    public bool IsAuthenticated => _client.IsAuthenticated;
    public DateTime LastUsed { get; private set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public MailKitSmtpConnection(MailKitConfiguration config, ILogger<MailKitSmtpConnection> logger)
    {
        _config = config;
        _logger = logger;
        _client = new SmtpClient
        {
            Timeout = (int)config.CommandTimeout.TotalMilliseconds
        };
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            var secureSocketOptions = _config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            using var timeoutCts = new CancellationTokenSource(_config.ConnectionTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await _client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, secureSocketOptions, combinedCts.Token);
        }
    }

    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
        {
            await _client.AuthenticateAsync(_config.Username, _config.Password, cancellationToken);
        }
    }

    public async Task<string> SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
    {
        LastUsed = DateTime.UtcNow;
        var messageId = await _client.SendAsync(message, cancellationToken);
        return messageId;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            await _client.DisconnectAsync(true, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}