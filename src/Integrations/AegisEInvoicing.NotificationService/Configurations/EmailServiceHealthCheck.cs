using AegisEInvoicing.NotificationService.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.NotificationService.Configurations;

public class EmailServiceHealthCheck(
    IEmailService emailService,
    ILogger<EmailServiceHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Azure Communication Services client is initialised at startup;
            // a non-null service instance indicates the provider is configured.
            return Task.FromResult(
                emailService is not null
                    ? HealthCheckResult.Healthy("Email service is configured")
                    : HealthCheckResult.Unhealthy("Email service is not registered"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email service health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Email service is unavailable", ex));
        }
    }
}