using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AegisEInvoicing.NotificationService.Configurations;

public class SmtpConnectionPool : ISmtpConnectionManager
{
    private readonly ConcurrentQueue<ISmtpConnection> _availableConnections;
    private readonly ConcurrentDictionary<string, ISmtpConnection> _allConnections;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly MailKitConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmtpConnectionPool> _logger;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public SmtpConnectionPool(IOptions<MailKitConfiguration> config, IServiceProvider serviceProvider,
        ILogger<SmtpConnectionPool> logger)
    {
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _availableConnections = new ConcurrentQueue<ISmtpConnection>();
        _allConnections = new ConcurrentDictionary<string, ISmtpConnection>();
        _connectionSemaphore = new SemaphoreSlim(_config.MaxConcurrentOperations);

        // Cleanup timer for idle connections
        _cleanupTimer = new Timer(CleanupIdleConnections, null,
            _config.ConnectionIdleTimeout, _config.ConnectionIdleTimeout);
    }

    public async Task<ISmtpConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);

        try
        {
            // Try to get existing connection
            if (_availableConnections.TryDequeue(out var connection))
            {
                if (ValidateConnectionHealth(connection))
                {
                    return connection;
                }
                else
                {
                    await SafeDisposeConnection(connection);
                }
            }

            // Create new connection
            connection = await CreateNewConnectionAsync(cancellationToken);
            return connection;
        }
        catch
        {
            _connectionSemaphore.Release();
            throw;
        }
    }

    public async Task ReturnConnectionAsync(ISmtpConnection connection, bool isHealthy = true)
    {
        try
        {
            if (isHealthy && !_disposed && _availableConnections.Count < _config.ConnectionPoolSize)
            {
                _availableConnections.Enqueue(connection);
            }
            else
            {
                await SafeDisposeConnection(connection);
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public bool ValidateConnectionHealth(ISmtpConnection connection)
    {
        try
        {
            return connection.IsConnected && connection.IsAuthenticated &&
                   DateTime.UtcNow - connection.LastUsed <= _config.ConnectionIdleTimeout;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection health check failed for {ConnectionId}", connection.ConnectionId);
            return false;
        }
    }

    private async Task<ISmtpConnection> CreateNewConnectionAsync(CancellationToken cancellationToken)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<MailKitSmtpConnection>>();
        var connection = new MailKitSmtpConnection(_config, logger);

        try
        {
            await connection.ConnectAsync(cancellationToken);
            await connection.AuthenticateAsync(cancellationToken);

            _allConnections.TryAdd(connection.ConnectionId, connection);

            _logger.LogDebug("Created new SMTP connection {ConnectionId}", connection.ConnectionId);
            return connection;
        }
        catch
        {
            await SafeDisposeConnection(connection);
            throw;
        }
    }

    private async Task SafeDisposeConnection(ISmtpConnection connection)
    {
        try
        {
            _allConnections.TryRemove(connection.ConnectionId, out _);
            await connection.DisconnectAsync().ConfigureAwait(false);
            connection.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing connection {ConnectionId}", connection.ConnectionId);
        }
    }

    private void SafeDisposeConnectionSync(ISmtpConnection connection)
    {
        try
        {
            _allConnections.TryRemove(connection.ConnectionId, out _);
            connection.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing connection {ConnectionId} synchronously", connection.ConnectionId);
        }
    }

    private void CleanupIdleConnections(object? state)
    {
        if (_disposed) return;

        // Run cleanup in a fire-and-forget task with proper exception handling
        _ = CleanupIdleConnectionsAsync();
    }

    private async Task CleanupIdleConnectionsAsync()
    {
        try
        {
            if (_disposed) return;

            var connectionsToRemove = new List<ISmtpConnection>();
            var tempList = new List<ISmtpConnection>();

            // Drain the queue to check connections
            while (_availableConnections.TryDequeue(out var connection))
            {
                tempList.Add(connection);
            }

            foreach (var connection in tempList)
            {
                if (DateTime.UtcNow - connection.LastUsed > _config.ConnectionIdleTimeout)
                {
                    connectionsToRemove.Add(connection);
                }
                else
                {
                    _availableConnections.Enqueue(connection);
                }
            }

            // Clean up idle connections
            foreach (var connection in connectionsToRemove)
            {
                await SafeDisposeConnection(connection).ConfigureAwait(false);
            }

            if (connectionsToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} idle connections", connectionsToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during idle connection cleanup");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cleanupTimer?.Dispose();
            _connectionSemaphore?.Dispose();

            // Dispose all connections synchronously during disposal
            foreach (var connection in _allConnections.Values)
            {
                SafeDisposeConnectionSync(connection);
            }
        }
    }
}