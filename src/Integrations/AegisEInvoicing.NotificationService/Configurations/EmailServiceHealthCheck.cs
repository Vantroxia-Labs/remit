using AegisEInvoicing.NotificationService.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.NotificationService.Configurations;

public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly ISmtpConnectionManager _connectionManager;
    private readonly ILogger<EmailServiceHealthCheck> _logger;

    public EmailServiceHealthCheck(ISmtpConnectionManager connectionManager,
        ILogger<EmailServiceHealthCheck> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connection acquisition
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var connection = await _connectionManager.GetConnectionAsync(combinedCts.Token);
            var isHealthy = _connectionManager.ValidateConnectionHealth(connection);
            await _connectionManager.ReturnConnectionAsync(connection, isHealthy);

            return isHealthy
                ? HealthCheckResult.Healthy("Email service is operational")
                : HealthCheckResult.Degraded("Email service connections are unhealthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email service health check failed");
            return HealthCheckResult.Unhealthy("Email service is unavailable", ex);
        }
    }
}