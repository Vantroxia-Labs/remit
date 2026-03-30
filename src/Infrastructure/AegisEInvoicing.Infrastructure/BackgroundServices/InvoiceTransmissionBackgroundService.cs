using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for processing invoice transmission queue
/// </summary>
public class InvoiceTransmissionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvoiceTransmissionBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30);

    public InvoiceTransmissionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<InvoiceTransmissionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice transmission background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IInvoiceTransmissionQueueService>();

                var processedCount = await queueService.ProcessPendingRequestsAsync(stoppingToken);

                if (processedCount > 0)
                {
                    _logger.LogInformation("Processed {Count} transmission requests", processedCount);
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in invoice transmission background service");

                // Wait before retrying to avoid tight loop on persistent errors
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Invoice transmission background service stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invoice transmission background service is stopping");
        await base.StopAsync(cancellationToken);
    }
}