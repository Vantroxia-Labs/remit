using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that refreshes FIRS reference data cache daily at 12:00 PM
/// </summary>
public class ReferenceDataRefreshBackgroundService(
    IReferenceDataCacheService cacheService,
    ILogger<ReferenceDataRefreshBackgroundService> logger) : BackgroundService
{
    private readonly IReferenceDataCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<ReferenceDataRefreshBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reference Data Refresh Background Service started");

        try
        {
            _logger.LogInformation("Performing initial reference data cache load (startup - max 60s timeout)");
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            await _cacheService.RefreshCacheAsync(cts.Token);
            
            _logger.LogInformation(
                "Initial cache load SUCCESSFUL. Application is ready to accept requests. " +
                "Cache health: {IsHealthy}",
                _cacheService.IsCacheHealthy());
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Timeout occurred (not application shutdown)
            _logger.LogCritical(
                "CRITICAL: Initial cache load TIMEOUT after 60 seconds. " +
                "FIRS API may be down. Application will start but may reject invoices until cache loads. " +
                "Next attempt in 15 minutes.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "CRITICAL: Initial cache load FAILED. " +
                "FIRS API may be unreachable. Application will start but may reject invoices until cache loads. " +
                "Next attempt in 15 minutes.");
        }

        // Continue with scheduled refresh loop (12 AM daily)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var next12AM = now.Date.AddDays(1); // Tomorrow at 12:00 AM

                var delay = next12AM - now;

                _logger.LogInformation(
                    "Next reference data cache refresh scheduled at {ScheduledTime} UTC (in {DelayHours:F1} hours)",
                    next12AM.ToUniversalTime(), delay.TotalHours);

                // Wait until 12 AM
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Refresh cache at 12 AM
                _logger.LogInformation("Executing scheduled reference data cache refresh at {Time} UTC", DateTime.UtcNow);
                
                try
                {
                    await _cacheService.RefreshCacheAsync(stoppingToken);
                    _logger.LogInformation("Scheduled cache refresh completed successfully");
                }
                catch (Exception ex)
                {
                    // Exception already logged by cache service
                    // Cache will keep using last known good data
                    _logger.LogWarning(ex, "Scheduled cache refresh failed. Using existing cache.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Reference data refresh background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reference data refresh background service scheduling. Will retry tomorrow.");
                
                // Wait 1 hour before next retry
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Reference Data Refresh Background Service stopped");
    }
}
