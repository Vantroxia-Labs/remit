using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AegisEInvoicing.SFTP.API.Controllers;

/// <summary>
/// Controller for managing and monitoring the background service
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BackgroundServiceController(
    IFileProcessingService fileProcessingService,
    ISftpService sftpService,
    ILogger<BackgroundServiceController> logger) : ControllerBase
{
    private readonly IFileProcessingService _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
    private readonly ISftpService _sftpService = sftpService ?? throw new ArgumentNullException(nameof(sftpService));
    private readonly ILogger<BackgroundServiceController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets the current status of the background service
    /// </summary>
    /// <returns>Service status information</returns>
    [HttpGet("status")]
    public async Task<ActionResult<ServiceStatus>> GetServiceStatus()
    {
        try
        {
            var status = await _fileProcessingService.GetServiceStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Error getting service status. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred while getting service status.", ErrorId = errorId });
        }
    }

    /// <summary>
    /// Manually triggers file processing for all connections
    /// </summary>
    /// <returns>Processing statistics</returns>
    [HttpPost("trigger")]
    public async Task<ActionResult<ProcessingStatistics>> TriggerProcessing()
    {
        try
        {
            _logger.LogInformation("Manual processing trigger requested");
            var statistics = await _fileProcessingService.ProcessAllPendingFilesAsync(HttpContext.RequestAborted);
            return Ok(statistics);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Manual processing was cancelled");
            return StatusCode(499, new { Error = "Processing was cancelled" });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Error during manual processing. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred during manual processing.", ErrorId = errorId });
        }
    }

    /// <summary>
    /// Manually triggers file processing for a specific connection
    /// </summary>
    /// <param name="connectionId">SFTP connection ID</param>
    /// <param name="maxFiles">Maximum number of files to process (optional)</param>
    /// <returns>Processing results</returns>
    [HttpPost("trigger/{connectionId}")]
    public async Task<ActionResult<List<FileProcessingResult>>> TriggerConnectionProcessing(
        string connectionId, 
        [FromQuery] int? maxFiles = null)
    {
        try
        {
            _logger.LogInformation("Manual processing trigger requested for connection: {ConnectionId}, MaxFiles: {MaxFiles}", 
                connectionId, maxFiles);
            
            var results = await _fileProcessingService.ProcessFilesFromConnectionAsync(
                connectionId, maxFiles, HttpContext.RequestAborted);
            
            return Ok(results);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Manual processing was cancelled for connection: {ConnectionId}", connectionId);
            return StatusCode(499, new { Error = "Processing was cancelled" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid connection ID: {ConnectionId}", connectionId);
            return BadRequest(new { Error = "Invalid connection ID provided." });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Error during manual processing for connection {ConnectionId}. ErrorId: {ErrorId}",
                connectionId, errorId);
            return StatusCode(500, new { Error = "An internal error occurred during connection processing.", ErrorId = errorId });
        }
    }

    /// <summary>
    /// Gets health status for all SFTP connections
    /// </summary>
    /// <returns>Health check results</returns>
    [HttpGet("health")]
    public async Task<ActionResult<Dictionary<string, bool>>> GetHealthStatus()
    {
        try
        {
            var healthChecks = await _fileProcessingService.PerformHealthChecksAsync(HttpContext.RequestAborted);
            return Ok(healthChecks);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new { Error = "Health check was cancelled" });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Error during health check. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred during health check.", ErrorId = errorId });
        }
    }

    /// <summary>
    /// Tests connectivity to a specific SFTP connection
    /// </summary>
    /// <param name="connectionId">SFTP connection ID</param>
    /// <returns>Connection test result</returns>
    [HttpGet("test-connection/{connectionId}")]
    public async Task<ActionResult<object>> TestConnection(string connectionId)
    {
        try
        {
            _logger.LogInformation("Testing connection: {ConnectionId}", connectionId);
            var isHealthy = await _sftpService.TestConnectionAsync(connectionId, HttpContext.RequestAborted);
            
            return Ok(new
            {
                ConnectionId = connectionId,
                IsHealthy = isHealthy,
                TestedAt = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new { Error = "Connection test was cancelled" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid connection ID: {ConnectionId}", connectionId);
            return BadRequest(new { Error = "Invalid connection ID provided." });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Error testing connection {ConnectionId}. ErrorId: {ErrorId}", connectionId, errorId);
            return StatusCode(500, new { Error = "An internal error occurred while testing connection.", ErrorId = errorId });
        }
    }

    /// <summary>
    /// Lists all configured SFTP connections
    /// </summary>
    /// <returns>List of SFTP connections</returns>
    [HttpGet("connections")]
    public async Task<ActionResult<object>> GetConnectionsAsync()
    {
        try
        {
            var allConnections = await _sftpService.GetAllConnectionsAsync().ConfigureAwait(false);
            var enabledConnections = await _sftpService.GetEnabledConnectionsAsync().ConfigureAwait(false);

            var connectionInfo = allConnections.Select(c => new
            {
                c.ConnectionId,
                c.Host,
                c.Port,
                c.WorkingDirectory,
                c.IsEnabled,
                c.Description
            });

            return Ok(new
            {
                TotalConnections = allConnections.Count,
                EnabledConnections = enabledConnections.Count,
                Connections = connectionInfo
            });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Error getting connections. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred while getting connections.", ErrorId = errorId });
        }
    }

    /// <summary>
    /// Triggers cleanup of old files
    /// </summary>
    /// <param name="olderThanDays">Remove files older than this many days (default: 30)</param>
    /// <returns>Number of files cleaned up</returns>
    [HttpPost("cleanup")]
    public async Task<ActionResult<object>> CleanupOldFiles([FromQuery] int olderThanDays = 30)
    {
        try
        {
            _logger.LogInformation("Manual cleanup requested for files older than {Days} days", olderThanDays);
            var cleanedCount = await _fileProcessingService.CleanupOldFilesAsync(olderThanDays, HttpContext.RequestAborted);
            
            return Ok(new
            {
                Message = "Cleanup completed successfully",
                FilesRemoved = cleanedCount,
                OlderThanDays = olderThanDays,
                CleanedAt = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new { Error = "Cleanup was cancelled" });
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Error during cleanup. ErrorId: {ErrorId}", errorId);
            return StatusCode(500, new { Error = "An internal error occurred during cleanup.", ErrorId = errorId });
        }
    }

    /// <summary>
    /// Gets service information and version
    /// </summary>
    /// <returns>Service information</returns>
    [HttpGet("info")]
    public ActionResult<object> GetServiceInfo()
    {
        return Ok(new
        {
            ServiceName = "EInvoice Integrator Background Service",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            WorkingDirectory = Environment.CurrentDirectory,
            StartTime = Process.GetCurrentProcess().StartTime,
            Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
            MemoryUsage = $"{GC.GetTotalMemory(false) / 1024 / 1024:F2} MB"
        });
    }
}

/// <summary>
/// Simple home controller for basic endpoints
/// </summary>
[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    /// <summary>
    /// Root endpoint
    /// </summary>
    [HttpGet]
    public ActionResult Get()
    {
        return Ok(new
        {
            Service = "EInvoice Integrator Background Service",
            Status = "Running",
            Timestamp = DateTime.UtcNow,
            Endpoints = new
            {
                Health = "/health",
                Status = "/status",
                ServiceInfo = "/api/v1/backgroundservice/info",
                ManualTrigger = "/api/v1/backgroundservice/trigger (POST)",
                Connections = "/api/v1/backgroundservice/connections"
            }
        });
    }
}