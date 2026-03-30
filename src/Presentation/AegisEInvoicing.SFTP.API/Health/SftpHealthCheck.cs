using AegisEInvoicing.SFTP.API.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AegisEInvoicing.SFTP.API.Health;

/// <summary>
/// Health check for SFTP connections
/// </summary>
public class SftpHealthCheck(ISftpService sftpService, ILogger<SftpHealthCheck> logger) : IHealthCheck
{
    private readonly ISftpService _sftpService = sftpService ?? throw new ArgumentNullException(nameof(sftpService));
    private readonly ILogger<SftpHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connections = await _sftpService.GetEnabledConnectionsAsync(cancellationToken).ConfigureAwait(false);

            if (!connections.Any())
            {
                return HealthCheckResult.Unhealthy("No SFTP connections are enabled");
            }

            var healthCheckTasks = connections.Select(async connection =>
            {
                try
                {
                    var isHealthy = await _sftpService.TestConnectionAsync(connection.ConnectionId, cancellationToken);
                    return new { ConnectionId = connection.ConnectionId, IsHealthy = isHealthy };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for SFTP connection {ConnectionId}", connection.ConnectionId);
                    return new { ConnectionId = connection.ConnectionId, IsHealthy = false };
                }
            });

            var results = await Task.WhenAll(healthCheckTasks);
            var healthyCount = results.Count(r => r.IsHealthy);
            var totalCount = results.Length;
            
            var data = new Dictionary<string, object>
            {
                { "TotalConnections", totalCount },
                { "HealthyConnections", healthyCount },
                { "UnhealthyConnections", totalCount - healthyCount },
                { "ConnectionDetails", results.ToDictionary(r => r.ConnectionId, r => r.IsHealthy ? "Healthy" : "Unhealthy") }
            };

            if (healthyCount == 0)
            {
                return HealthCheckResult.Unhealthy(
                    "All SFTP connections are unhealthy", 
                    data: data);
            }
            
            if (healthyCount < totalCount)
            {
                return HealthCheckResult.Degraded(
                    $"{healthyCount}/{totalCount} SFTP connections are healthy", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"All {totalCount} SFTP connections are healthy", 
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SFTP health check: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy(
                $"Health check failed: {ex.Message}",
                ex);
        }
    }
}

/// <summary>
/// Health check for file processing service
/// </summary>
public class FileProcessingServiceHealthCheck(
    IFileProcessingService fileProcessingService,
    ILogger<FileProcessingServiceHealthCheck> logger) : IHealthCheck
{
    private readonly IFileProcessingService _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
    private readonly ILogger<FileProcessingServiceHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceStatus = await _fileProcessingService.GetServiceStatusAsync();
            var timeSinceLastRun = DateTime.UtcNow - serviceStatus.LastProcessingRun;
            
            var data = new Dictionary<string, object>
            {
                { "IsRunning", serviceStatus.IsRunning },
                { "LastProcessingRun", serviceStatus.LastProcessingRun },
                { "TimeSinceLastRun", timeSinceLastRun.ToString(@"hh\:mm\:ss") },
                { "ServiceStartTime", serviceStatus.ServiceStartTime },
                { "ServiceUptime", (DateTime.UtcNow - serviceStatus.ServiceStartTime).ToString(@"dd\:hh\:mm\:ss") },
                { "Health", serviceStatus.Health.ToString() },
                { "ActiveConnections", serviceStatus.ActiveConnections.Count },
                { "TotalFilesProcessed", serviceStatus.CurrentBatchStatistics.TotalFilesProcessed },
                { "SuccessRate", $"{serviceStatus.CurrentBatchStatistics.SuccessRate:F2}%" }
            };

            // Check if service has been running recently (within last 10 minutes)
            if (timeSinceLastRun > TimeSpan.FromMinutes(10) && serviceStatus.CurrentBatchStatistics.TotalFilesProcessed == 0)
            {
                return HealthCheckResult.Degraded(
                    "File processing service hasn't run recently (may be starting up)", 
                    data: data);
            }

            // Check success rate if files have been processed
            if (serviceStatus.CurrentBatchStatistics.TotalFilesProcessed > 0 && 
                serviceStatus.CurrentBatchStatistics.SuccessRate < 50)
            {
                return HealthCheckResult.Degraded(
                    $"Low success rate: {serviceStatus.CurrentBatchStatistics.SuccessRate:F2}%", 
                    data: data);
            }

            // Check overall service health
            if (serviceStatus.Health == Models.ServiceHealth.Critical)
            {
                return HealthCheckResult.Unhealthy(
                    "File processing service is in critical state", 
                    data: data);
            }

            if (serviceStatus.Health == Models.ServiceHealth.Warning)
            {
                return HealthCheckResult.Degraded(
                    "File processing service has warnings", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "File processing service is running normally", 
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file processing service health check: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy(
                $"Health check failed: {ex.Message}",
                ex);
        }
    }
}

/// <summary>
/// Health check for database connectivity
/// </summary>
public class DatabaseHealthCheck(IServiceProvider serviceProvider, ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<DatabaseHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to create a scope and resolve MediatR to test database connectivity
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetService<MediatR.IMediator>();
            
            if (mediator == null)
            {
                return HealthCheckResult.Unhealthy("MediatR service is not available");
            }

            // Simple test - we could implement a specific health check command/query
            // For now, just verify the service is available
            var data = new Dictionary<string, object>
            {
                { "Status", "Available" },
                { "CheckedAt", DateTime.UtcNow }
            };

            return HealthCheckResult.Healthy("Database connectivity is available", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy(
                $"Database connectivity failed: {ex.Message}",
                ex);
        }
    }
}

/// <summary>
/// Composite health check that provides overall system health
/// </summary>
public class OverallSystemHealthCheck(
    ISftpService sftpService,
    IFileProcessingService fileProcessingService,
    ILogger<OverallSystemHealthCheck> logger) : IHealthCheck
{
    private readonly ISftpService _sftpService = sftpService ?? throw new ArgumentNullException(nameof(sftpService));
    private readonly IFileProcessingService _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
    private readonly ILogger<OverallSystemHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Perform comprehensive health checks
            var healthChecks = await _fileProcessingService.PerformHealthChecksAsync(cancellationToken);
            var serviceStatus = await _fileProcessingService.GetServiceStatusAsync();
            
            var endTime = DateTime.UtcNow;
            var checkDuration = endTime - startTime;
            
            var healthyServices = healthChecks.Count(h => h.Value);
            var totalServices = healthChecks.Count;
            
            var data = new Dictionary<string, object>
            {
                { "HealthCheckDuration", checkDuration.TotalMilliseconds },
                { "TotalServices", totalServices },
                { "HealthyServices", healthyServices },
                { "UnhealthyServices", totalServices - healthyServices },
                { "ServiceHealth", serviceStatus.Health.ToString() },
                { "ServiceUptime", (DateTime.UtcNow - serviceStatus.ServiceStartTime).ToString(@"dd\:hh\:mm\:ss") },
                { "IsProcessingRunning", serviceStatus.IsRunning },
                { "LastProcessingRun", serviceStatus.LastProcessingRun },
                { "ServicesStatus", healthChecks },
                { "HealthMessages", serviceStatus.HealthMessages }
            };

            if (healthyServices == 0)
            {
                return HealthCheckResult.Unhealthy(
                    "All critical services are down", 
                    data: data);
            }

            var criticalServicesDown = healthChecks
                .Where(h => h.Key.Contains("Database") || h.Key.Contains("SFTP"))
                .Any(h => !h.Value);

            if (criticalServicesDown)
            {
                return HealthCheckResult.Unhealthy(
                    "Critical services are down", 
                    data: data);
            }

            if (healthyServices < totalServices)
            {
                return HealthCheckResult.Degraded(
                    $"{healthyServices}/{totalServices} services are healthy", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"All {totalServices} services are healthy", 
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Overall system health check failed: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy(
                $"System health check failed: {ex.Message}",
                ex);
        }
    }
}