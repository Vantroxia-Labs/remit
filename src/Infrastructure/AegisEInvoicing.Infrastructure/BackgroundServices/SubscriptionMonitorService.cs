using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.BackgroundServices;

public class SubscriptionMonitorService(
    IServiceProvider serviceProvider,
    ILogger<SubscriptionMonitorService> logger,
    IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<SubscriptionMonitorService> _logger = logger;
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute
    private readonly TimeSpan _criticalCheckInterval = TimeSpan.FromSeconds(30); // More frequent when near expiry

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Monitor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseValidationService>();

                // Check system configuration first
                var systemConfig = await context.SystemConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(stoppingToken);

                if (systemConfig == null)
                {
                    _logger.LogWarning("System configuration not found. Waiting for setup...");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                // For On-Premise deployments, validate license
                if (systemConfig.DeploymentMode == DeploymentMode.OnPremise)
                {
                    var isLicenseValid = await licenseService.ValidateLicenseAsync(stoppingToken);
                    
                    if (!isLicenseValid)
                    {
                        _logger.LogCritical("License validation failed. Initiating application shutdown.");
                        await LogShutdownReasonAsync(context, "License expired or invalid", stoppingToken);
                        licenseService.ShutdownApplication("License validation failed");
                        return;
                    }

                    // Check if license is expiring soon
                    if (systemConfig.LicenseExpiryDate.HasValue)
                    {
                        var daysUntilExpiry = (systemConfig.LicenseExpiryDate.Value - DateTimeOffset.UtcNow).Days;
                        
                        if (daysUntilExpiry <= 7)
                        {
                            _logger.LogWarning("License expiring in {Days} days", daysUntilExpiry);
                            
                            // Send notification if within 7 days
                            await SendExpiryNotificationAsync(context, daysUntilExpiry, stoppingToken);
                        }

                        // Use more frequent checks when near expiry
                        if (daysUntilExpiry <= 1)
                        {
                            await Task.Delay(_criticalCheckInterval, stoppingToken);
                            continue;
                        }
                    }
                }

                // For SaaS deployments, check all business subscriptions
                if (systemConfig.DeploymentMode == DeploymentMode.Cloud)
                {
                    var businesses = await context.Businesses
                        .Include(b => b.Subscriptions)
                        .Where(b => b.Status == Domain.Enums.BusinessStatus.Active)
                        .ToListAsync(stoppingToken);

                    foreach (var business in businesses)
                    {
                        if (!business.Subscriptions.Any())
                        {
                            _logger.LogWarning("Business {BusinessId} has no subscription", business.Id);
                            await SuspendBusinessAsync(context, business, "No active subscription", stoppingToken);
                            continue;
                        }

                        // Business is OK if at least one subscription is still active/in grace period
                        if (business.Subscriptions.All(s => s.IsExpired()))
                        {
                            if (!business.Subscriptions.Any(s => s.IsGracePeriod()))
                            {
                                _logger.LogWarning("Business {BusinessId} all subscriptions expired beyond grace period", business.Id);
                                await SuspendBusinessAsync(context, business, "Subscription expired", stoppingToken);
                            }
                            else
                            {
                                var daysOverdue = business.Subscriptions.Max(s => s.DaysOverdue());
                                _logger.LogWarning("Business {BusinessId} is in grace period. Days overdue: {Days}",
                                    business.Id, daysOverdue);
                            }
                        }

                        // Warn if any subscription is expiring soon
                        var daysUntilExpiry = business.Subscriptions.Min(s => s.DaysUntilExpiry());
                        if (daysUntilExpiry <= 7 && daysUntilExpiry > 0)
                        {
                            _logger.LogInformation("Business {BusinessId} subscription expiring in {Days} days",
                                business.Id, daysUntilExpiry);
                        }
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription monitoring");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait before retry
            }
        }
    }

    private async Task SuspendBusinessAsync(
        IApplicationDbContext context, 
        Business business, 
        string reason, 
        CancellationToken cancellationToken)
    {
        try
        {
            business.Deactivate(business.CreatedBy);
            context.Businesses.Update(business);
            await context.SaveChangesAsync(cancellationToken);
            
            _logger.LogWarning("Business {BusinessId} suspended: {Reason}", business.Id, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suspend business {BusinessId}", business.Id);
        }
    }

    private async Task LogShutdownReasonAsync(
        IApplicationDbContext context,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = IntegrationLog.Create(
                "SystemShutdown",
                "System",
                $"Shutdown initiated at {DateTimeOffset.UtcNow:O}");
            log.MarkAsCompleted(reason, false, reason);

            context.IntegrationLogs.Add(log);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log shutdown reason");
        }
    }

    private async Task SendExpiryNotificationAsync(
        IApplicationDbContext context,
        int daysUntilExpiry,
        CancellationToken cancellationToken)
    {
        try
        {
            // Log notification (in production, this would send email/SMS)
            var log = IntegrationLog.Create(
                "LicenseExpiryNotification",
                "System",
                $"License expiring in {daysUntilExpiry} days");
            log.MarkAsCompleted($"License expiring in {daysUntilExpiry} days", true);

            context.IntegrationLogs.Add(log);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiry notification");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription Monitor Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}