using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Backup scheduler service that ensures file processing continues even if Quartz scheduler fails
/// </summary>
public class BackupSchedulerService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProcessingConfiguration _processingConfig;
    private readonly ILogger<BackupSchedulerService> _logger;
    private bool _isQuartzRunning = true;
    private DateTime _lastQuartzActivity = DateTime.UtcNow;
    private readonly SemaphoreSlim _executionSemaphore = new(1, 1);

    public BackupSchedulerService(
        IServiceProvider serviceProvider,
        IOptions<ProcessingConfiguration> processingConfig,
        ILogger<BackupSchedulerService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _processingConfig = processingConfig.Value ?? throw new ArgumentNullException(nameof(processingConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void NotifyQuartzActivity()
    {
        _lastQuartzActivity = DateTime.UtcNow;
        if (!_isQuartzRunning)
        {
            _logger.LogInformation("[BACKUP SCHEDULER] Quartz activity detected - disabling backup processing");
            _isQuartzRunning = true;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BACKUP SCHEDULER] Backup scheduler service started");

        // Wait a bit for Quartz to initialize
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckQuartzHealth(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Check every 30 seconds
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("[BACKUP SCHEDULER] Backup scheduler service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BACKUP SCHEDULER] Error in backup scheduler: {Message}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task CheckQuartzHealth(CancellationToken cancellationToken)
    {
        var timeSinceLastActivity = DateTime.UtcNow - _lastQuartzActivity;
        var expectedInterval = TimeSpan.FromSeconds(_processingConfig.ProcessingIntervalSeconds);
        var alertThreshold = expectedInterval.Add(TimeSpan.FromSeconds(60)); // Give 60 seconds grace period

        if (timeSinceLastActivity > alertThreshold)
        {
            if (_isQuartzRunning)
            {
                _logger.LogWarning("[BACKUP SCHEDULER] Quartz appears to be inactive. Last activity: {LastActivity}, Time since: {TimeSince}. Activating backup processing.",
                    _lastQuartzActivity, timeSinceLastActivity);
                _isQuartzRunning = false;
            }

            // Execute backup processing
            await ExecuteBackupProcessing(cancellationToken);
        }
    }

    private async Task ExecuteBackupProcessing(CancellationToken cancellationToken)
    {
        if (!await _executionSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            _logger.LogDebug("[BACKUP SCHEDULER] Backup processing already running, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("[BACKUP SCHEDULER] Executing backup file processing");

            using var scope = _serviceProvider.CreateScope();
            var fileProcessingService = scope.ServiceProvider.GetRequiredService<IFileProcessingService>();

            var statistics = await fileProcessingService.ProcessAllPendingFilesAsync(cancellationToken);

            _logger.LogInformation("[BACKUP SCHEDULER] Backup processing completed. Files processed: {TotalFiles}, Success: {SuccessCount}, Errors: {ErrorCount}",
                statistics.TotalFilesProcessed,
                statistics.SuccessfulFiles,
                statistics.ErrorFiles);

            // Update last activity to current time since we just processed
            _lastQuartzActivity = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP SCHEDULER] Error during backup processing: {Message}", ex.Message);
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    public override void Dispose()
    {
        _executionSemaphore?.Dispose();
        base.Dispose();
    }
}