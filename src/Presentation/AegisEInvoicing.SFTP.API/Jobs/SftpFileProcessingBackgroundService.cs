
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using AegisEInvoicing.SFTP.API.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using System.Diagnostics;

namespace AegisEInvoicing.SFTP.API.Jobs
{
    public class SftpFileProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SftpFileProcessingBackgroundService> _logger;
        private readonly ProcessingConfiguration _processingConfig;
        private readonly SemaphoreSlim _processingSemaphore;
        
        // Performance metrics
        private int _consecutiveEmptyRuns = 0;
        private const int EmptyRunThreshold = 3;
        private TimeSpan _adaptiveInterval;
        
        public SftpFileProcessingBackgroundService(
            ILogger<SftpFileProcessingBackgroundService> logger, 
            IServiceScopeFactory serviceScopeFactory,
            IOptions<ProcessingConfiguration> processingConfig)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _processingConfig = processingConfig.Value ?? throw new ArgumentNullException(nameof(processingConfig));
            
            // Use adaptive interval starting with configured interval
            _adaptiveInterval = TimeSpan.FromSeconds(_processingConfig.ProcessingIntervalSeconds);
            
            // Prevent concurrent processing runs
            _processingSemaphore = new SemaphoreSlim(1, 1);
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SFTP file processing background service started (SFTPGo mode) with interval: {IntervalSeconds}s", 
                _processingConfig.ProcessingIntervalSeconds);
            
            try
            {
                // Wait for initial startup delay to allow other services to initialize
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Skip if previous processing is still running
                    if (!await _processingSemaphore.WaitAsync(100, stoppingToken))
                    {
                        _logger.LogWarning("[SFTP JOB] Previous processing still running, skipping this cycle");
                        await Task.Delay(_adaptiveInterval, stoppingToken);
                        continue;
                    }

                    try
                    {
                        await ProcessFilesWithMetricsAsync(stoppingToken);
                    }
                    finally
                    {
                        _processingSemaphore.Release();
                    }
                    
                    // Wait for next cycle
                    await Task.Delay(_adaptiveInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SFTP transmission background service shutdown requested");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error in SFTP transmission background service");
                throw;
            }
            finally
            {
                _logger.LogInformation("SFTP transmission background service stopped");
                _processingSemaphore.Dispose();
            }
        }
        
        private async Task ProcessFilesWithMetricsAsync(CancellationToken stoppingToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Create a service scope with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(5)); // Prevent runaway processing
                
                using var scope = _serviceScopeFactory.CreateScope();
                var fileProcessingService = scope.ServiceProvider.GetRequiredService<IFileProcessingService>();
                
                _logger.LogDebug("[SFTP JOB] Starting file processing cycle...");
                var statistics = await fileProcessingService.ProcessAllPendingFilesAsync(timeoutCts.Token);
                
                stopwatch.Stop();
                
                // Update adaptive timing based on results
                UpdateAdaptiveInterval(statistics);
                
                // Enhanced logging with performance metrics
                LogProcessingResults(statistics, stopwatch.Elapsed);
                
                // Warn about long processing times
                if (stopwatch.Elapsed > _adaptiveInterval)
                {
                    _logger.LogWarning("[SFTP JOB] Processing took {Duration}ms, longer than interval {Interval}ms. Consider optimization.",
                        stopwatch.Elapsed.TotalMilliseconds, _adaptiveInterval.TotalMilliseconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("[SFTP JOB] Processing cancelled by shutdown request");
                throw;
            }
            catch (TimeoutException)
            {
                _logger.LogError("[SFTP JOB] Processing timed out after 5 minutes");
                // Increase interval to avoid rapid retries
                _adaptiveInterval = TimeSpan.FromMinutes(2);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[SFTP JOB] Error during processing cycle (Duration: {Duration}ms)", 
                    stopwatch.Elapsed.TotalMilliseconds);
                
                // Implement exponential backoff for consecutive failures
                _consecutiveEmptyRuns = 0; // Reset empty run counter on error
                _adaptiveInterval = TimeSpan.FromMinutes(1); // Fallback interval
            }
        }
        
        private void UpdateAdaptiveInterval(Models.ProcessingStatistics statistics)
        {
            var baseInterval = TimeSpan.FromSeconds(_processingConfig.ProcessingIntervalSeconds);
            
            if (statistics.TotalFilesProcessed == 0)
            {
                _consecutiveEmptyRuns++;
                
                // Gradually increase interval when no files are found
                if (_consecutiveEmptyRuns >= EmptyRunThreshold)
                {
                    var multiplier = Math.Min(_consecutiveEmptyRuns / EmptyRunThreshold, 4); // Max 4x interval
                    _adaptiveInterval = TimeSpan.FromTicks(baseInterval.Ticks * multiplier);
                    
                    if (_consecutiveEmptyRuns == EmptyRunThreshold)
                    {
                        _logger.LogInformation("[SFTP JOB] No files found in {EmptyRuns} consecutive runs, increasing interval to {NewInterval}s",
                            _consecutiveEmptyRuns, _adaptiveInterval.TotalSeconds);
                    }
                }
            }
            else
            {
                // Reset to base interval when files are found
                if (_consecutiveEmptyRuns > 0)
                {
                    _logger.LogInformation("[SFTP JOB] Files found, resetting interval to {BaseInterval}s", 
                        baseInterval.TotalSeconds);
                    _consecutiveEmptyRuns = 0;
                    _adaptiveInterval = baseInterval;
                }
            }
            
            // Adaptive interval is now updated directly in _adaptiveInterval field
        }
        
        private void LogProcessingResults(Models.ProcessingStatistics statistics, TimeSpan duration)
        {
            if (statistics.TotalFilesProcessed > 0)
            {
                _logger.LogInformation(
                    "[SFTP JOB] Completed in {Duration:F1}s | Files: {Total} | Success: {Success} | Failed: {Failed} | Rate: {Rate:F1}% | Next: {NextInterval}s",
                    duration.TotalSeconds,
                    statistics.TotalFilesProcessed,
                    statistics.SuccessfulFiles,
                    statistics.ErrorFiles,
                    statistics.SuccessRate,
                    _adaptiveInterval.TotalSeconds);
                
                // Log error details if significant
                if (statistics.ErrorFiles > 0 && statistics.SuccessRate < 80)
                {
                    _logger.LogWarning("[SFTP JOB] Low success rate: {Rate:F1}%. Errors: {@ErrorSummary}",
                        statistics.SuccessRate, statistics.ErrorSummary);
                }
            }
            else
            {
                _logger.LogDebug("[SFTP JOB] No files processed in {Duration:F1}s | Next check in {NextInterval}s",
                    duration.TotalSeconds, _adaptiveInterval.TotalSeconds);
            }
        }
    }
}

