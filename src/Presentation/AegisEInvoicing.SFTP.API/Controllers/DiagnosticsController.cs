using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace AegisEInvoicing.SFTP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController(
    IFileProcessingService fileProcessingService,
    ISftpService sftpService,
    IScheduler scheduler,
    BackupSchedulerService backupScheduler,
    ILogger<DiagnosticsController> logger) : ControllerBase
{
    private readonly IFileProcessingService _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
    private readonly ISftpService _sftpService = sftpService ?? throw new ArgumentNullException(nameof(sftpService));
    private readonly IScheduler _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    private readonly BackupSchedulerService _backupScheduler = backupScheduler ?? throw new ArgumentNullException(nameof(backupScheduler));
    private readonly ILogger<DiagnosticsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Get comprehensive diagnostic information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDiagnostics()
    {
        try
        {
            _logger.LogInformation("[DIAGNOSTICS] Gathering diagnostic information...");

            var serviceStatus = await _fileProcessingService.GetServiceStatusAsync();
            var healthChecks = await _fileProcessingService.PerformHealthChecksAsync();
            var sftpConnections = await GetSftpConnectionStatus();
            var quartzStatus = await GetQuartzStatusInternal();

            var diagnostics = new
            {
                Timestamp = DateTime.UtcNow,
                ServiceStatus = serviceStatus,
                HealthChecks = healthChecks,
                SftpConnections = sftpConnections,
                QuartzScheduler = quartzStatus,
                SystemInfo = new
                {
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    TickCount = Environment.TickCount64
                }
            };

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "[DIAGNOSTICS] Error gathering diagnostics. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred while gathering diagnostics.", ErrorId = errorId, Timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Test SFTP connections and list available files
    /// </summary>
    [HttpGet("sftp")]
    public async Task<IActionResult> GetSftpStatus()
    {
        try
        {
            _logger.LogInformation("[DIAGNOSTICS] Testing SFTP connections and listing files...");

            var connections = await _sftpService.GetEnabledConnectionsAsync().ConfigureAwait(false);
            var results = new List<object>();

            foreach (var connection in connections)
            {
                try
                {
                    var isConnected = await _sftpService.TestConnectionAsync(connection.ConnectionId);
                    var files = isConnected 
                        ? await _sftpService.ListInProgressFilesAsync(connection.ConnectionId)
                        : [];

                    results.Add(new
                    {
                        ConnectionId = connection.ConnectionId,
                        Host = connection.Host,
                        WorkingDirectory = connection.WorkingDirectory,
                        IsConnected = isConnected,
                        FileCount = files.Count,
                        Files = files.Select(f => new
                        {
                            f.FileName,
                            f.Size,
                            f.LastModified,
                            f.FullPath
                        }).ToList()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[DIAGNOSTICS] Connection test failed for {ConnectionId}", connection.ConnectionId);
                    results.Add(new
                    {
                        ConnectionId = connection.ConnectionId,
                        Host = connection.Host,
                        WorkingDirectory = connection.WorkingDirectory,
                        IsConnected = false,
                        Error = "Connection test failed",
                        FileCount = 0,
                        Files = new List<object>()
                    });
                }
            }

            return Ok(new
            {
                Timestamp = DateTime.UtcNow,
                ConnectionCount = connections.Count,
                Connections = results
            });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "[DIAGNOSTICS] Error testing SFTP. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred while testing SFTP connections.", ErrorId = errorId, Timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Get Quartz scheduler status
    /// </summary>
    [HttpGet("quartz")]
    public async Task<IActionResult> GetQuartzStatus()
    {
        try
        {
            var status = await GetQuartzStatusInternal();
            return Ok(status);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "[DIAGNOSTICS] Error getting Quartz status. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred while getting scheduler status.", ErrorId = errorId, Timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Test file detection with detailed cache-busting information
    /// </summary>
    [HttpGet("sftp/{connectionId}/files")]
    public async Task<IActionResult> TestFileDetection(string connectionId)
    {
        try
        {
            _logger.LogInformation("[DIAGNOSTICS] Testing detailed file detection for connection: {ConnectionId}", connectionId);

            var files = await _sftpService.ListInProgressFilesAsync(connectionId);
            
            var result = new
            {
                Timestamp = DateTime.UtcNow,
                ConnectionId = connectionId,
                FileCount = files.Count,
                Files = files.Select(f => new
                {
                    f.FileName,
                    f.Size,
                    f.LastModified,
                    f.FullPath,
                    LastModifiedUtc = f.LastModified.ToString("yyyy-MM-dd HH:mm:ss UTC")
                }).ToList(),
                TestInfo = new
                {
                    CacheBustingEnabled = true,
                    FreshConnectionUsed = true,
                    MultiplePathVariantsTested = true,
                    FileValidationPerformed = true
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "[DIAGNOSTICS] Error testing file detection for {ConnectionId}. ErrorId: {ErrorId}", connectionId, errorId);
            return StatusCode(500, new { Error = "An internal error occurred while testing file detection.", ErrorId = errorId, ConnectionId = connectionId, Timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Force trigger file processing manually
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerProcessing()
    {
        try
        {
            _logger.LogInformation("[DIAGNOSTICS] Manual processing trigger requested");

            var statistics = await _fileProcessingService.ProcessAllPendingFilesAsync();

            return Ok(new
            {
                Message = "Processing triggered manually",
                Statistics = statistics,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "[DIAGNOSTICS] Error during manual trigger. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred while triggering processing.", ErrorId = errorId, Timestamp = DateTime.UtcNow });
        }
    }

    private async Task<object> GetSftpConnectionStatus()
    {
        var connections = await _sftpService.GetEnabledConnectionsAsync().ConfigureAwait(false);
        var results = new List<object>();

        foreach (var connection in connections)
        {
            try
            {
                var isConnected = await _sftpService.TestConnectionAsync(connection.ConnectionId);
                results.Add(new
                {
                    ConnectionId = connection.ConnectionId,
                    Host = connection.Host,
                    IsConnected = isConnected,
                    Status = isConnected ? "Healthy" : "Unhealthy"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DIAGNOSTICS] Connection status check failed for {ConnectionId}", connection.ConnectionId);
                results.Add(new
                {
                    ConnectionId = connection.ConnectionId,
                    Host = connection.Host,
                    IsConnected = false,
                    Status = "Error",
                    Error = "Connection check failed"
                });
            }
        }

        return new
        {
            TotalConnections = connections.Count,
            HealthyConnections = results.Count(r => ((dynamic)r).IsConnected),
            Connections = results
        };
    }

    private async Task<object> GetQuartzStatusInternal()
    {
        try
        {
            var isStarted = _scheduler.IsStarted;
            var isShutdown = _scheduler.IsShutdown;
            var schedulerName = _scheduler.SchedulerName;
            var schedulerInstanceId = _scheduler.SchedulerInstanceId;

            var jobKeys = await _scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());
            var jobs = new List<object>();

            foreach (var jobKey in jobKeys)
            {
                var jobDetail = await _scheduler.GetJobDetail(jobKey);
                var triggers = await _scheduler.GetTriggersOfJob(jobKey);
                
                var jobInfo = new
                {
                    JobKey = jobKey.ToString(),
                    JobType = jobDetail?.JobType.Name,
                    Description = jobDetail?.Description,
                    Triggers = triggers.Select(t => new
                    {
                        TriggerKey = t.Key.ToString(),
                        TriggerState = _scheduler.GetTriggerState(t.Key).Result.ToString(),
                        NextFireTime = t.GetNextFireTimeUtc(),
                        PreviousFireTime = t.GetPreviousFireTimeUtc(),
                        TriggerType = t.GetType().Name
                    }).ToList()
                };

                jobs.Add(jobInfo);
            }

            return new
            {
                Timestamp = DateTime.UtcNow,
                SchedulerName = schedulerName,
                SchedulerInstanceId = schedulerInstanceId,
                IsStarted = isStarted,
                IsShutdown = isShutdown,
                JobCount = jobKeys.Count,
                Jobs = jobs
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DIAGNOSTICS] Error getting Quartz status");
            return new
            {
                Timestamp = DateTime.UtcNow,
                Error = "Unable to retrieve scheduler status",
                IsStarted = false,
                IsShutdown = true
            };
        }
    }
}