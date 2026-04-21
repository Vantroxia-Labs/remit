using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AegisEInvoicing.ERP.API.HealthChecks;

public class SubscriptionHealthCheck(
    IServiceProvider serviceProvider,
    ILogger<SubscriptionHealthCheck> logger) : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<SubscriptionHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseValidationService>();

            var systemConfig = await dbContext.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (systemConfig == null)
            {
                return HealthCheckResult.Degraded("System configuration not found");
            }

            var data = new Dictionary<string, object>
            {
                ["deploymentMode"] = systemConfig.DeploymentMode.ToString()
            };

            // Check On-Premise license
            if (systemConfig.DeploymentMode == DeploymentMode.OnPremise)
            {
                var licenseInfo = await licenseService.GetLicenseInfoAsync(cancellationToken);
                
                data["licenseValid"] = licenseInfo.IsValid;
                data["licenseExpiry"] = licenseInfo.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A";
                data["daysRemaining"] = licenseInfo.DaysRemaining;

                if (!licenseInfo.IsValid)
                {
                    return HealthCheckResult.Unhealthy(
                        "License is invalid or expired",
                        data: data);
                }

                if (licenseInfo.DaysRemaining <= 7)
                {
                    return HealthCheckResult.Degraded(
                        $"License expiring in {licenseInfo.DaysRemaining} days",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"License valid for {licenseInfo.DaysRemaining} days",
                    data);
            }

            // Check SaaS subscriptions
            if (systemConfig.DeploymentMode == DeploymentMode.Cloud)
            {
                var activeBusinesses = await dbContext.Businesses
                    .Include(b => b.Subscriptions)
                    .Where(b => b.Status == AegisEInvoicing.Domain.Enums.BusinessStatus.Active)
                    .ToListAsync(cancellationToken);

                var totalBusinesses = activeBusinesses.Count;
                var expiredSubscriptions = 0;
                var expiringSubscriptions = 0;
                var activeSubscriptions = 0;

                foreach (var business in activeBusinesses)
                {
                    var primary = business.GetPrimarySubscription();
                    if (primary == null || primary.IsExpired())
                    {
                        expiredSubscriptions++;
                    }
                    else if (primary.DaysUntilExpiry() <= 7)
                    {
                        expiringSubscriptions++;
                    }
                    else
                    {
                        activeSubscriptions++;
                    }
                }

                data["totalBusinesses"] = totalBusinesses;
                data["activeSubscriptions"] = activeSubscriptions;
                data["expiringSubscriptions"] = expiringSubscriptions;
                data["expiredSubscriptions"] = expiredSubscriptions;

                if (expiredSubscriptions > 0)
                {
                    return HealthCheckResult.Degraded(
                        $"{expiredSubscriptions} businesses have expired subscriptions",
                        data: data);
                }

                if (expiringSubscriptions > 0)
                {
                    return HealthCheckResult.Healthy(
                        $"{expiringSubscriptions} subscriptions expiring soon",
                        data);
                }

                return HealthCheckResult.Healthy(
                    $"All {activeSubscriptions} subscriptions are active",
                    data);
            }

            return HealthCheckResult.Healthy("Subscription system operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription health");
            return HealthCheckResult.Unhealthy(
                "Error checking subscription status",
                exception: ex);
        }
    }
}