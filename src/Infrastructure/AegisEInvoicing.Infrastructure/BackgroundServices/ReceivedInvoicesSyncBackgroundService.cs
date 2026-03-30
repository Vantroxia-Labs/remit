using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Commands.SyncReceivedInvoices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically syncs received invoices for all active businesses
/// Syncs invoices for the current day only (businesses can receive invoices every minute)
/// Runs on a configurable schedule with batching and error handling
/// </summary>
public sealed class ReceivedInvoicesSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReceivedInvoicesSyncBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    // Configuration values (loaded from appsettings.json)
    private readonly bool _enabled;
    private readonly int _intervalMinutes;
    private readonly int _batchSize;
    private readonly int _delayBetweenBatchesMs;

    public ReceivedInvoicesSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReceivedInvoicesSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Load configuration
        _enabled = _configuration.GetValue<bool>("ReceivedInvoicesSync:Enabled", true);
        _intervalMinutes = _configuration.GetValue<int>("ReceivedInvoicesSync:IntervalMinutes", 1);
        _batchSize = _configuration.GetValue<int>("ReceivedInvoicesSync:BatchSize", 10);
        _delayBetweenBatchesMs = _configuration.GetValue<int>("ReceivedInvoicesSync:DelayBetweenBatchesMs", 5000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Received Invoices Sync Background Service is disabled");
            return;
        }

        _logger.LogInformation(
            "Received Invoices Sync Background Service started. Interval: {Hours} hours, Batch Size: {BatchSize}",
            _intervalMinutes, _batchSize);

        try
        {
            // Wait a bit before starting to allow the application to fully initialize
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Received Invoices Sync Background Service cancelled during initial delay");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting received invoices sync cycle");

                await SyncAllBusinessesAsync(stoppingToken);

                _logger.LogInformation(
                    "Completed received invoices sync cycle. Next sync in {Hours} hours",
                    _intervalMinutes);

                // Wait for the configured interval
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Received Invoices Sync Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in received invoices sync cycle: {Message}",
                    ex.Message);

                // Wait before retrying to avoid tight error loops
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Syncs received invoices for all active businesses with batching
    /// </summary>
    private async Task SyncAllBusinessesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        try
        {
            // Get all active businesses with valid TINs
            var businesses = await context.Businesses
                .AsNoTracking()
                .Where(b => !b.IsDeleted && b.Status == Domain.Enums.BusinessStatus.Active)
                .Select(b => new { b.Id, b.Name, TIN = b.TaxIdentificationNumber.Value })
                .ToListAsync(cancellationToken);

            if (!businesses.Any())
            {
                _logger.LogInformation("No active businesses found to sync");
                return;
            }

            _logger.LogInformation("Found {Count} active businesses to sync", businesses.Count);

            // Calculate date range for sync - same day only (businesses can receive invoices every minute)
            var syncDate = DateTime.UtcNow.Date;
            var dateForm = new DateTime(syncDate.Year, syncDate.Month, 1);
            var startDateStr = dateForm.ToString("yyyy-MM-dd");
            var endDateStr = dateForm.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");

            var totalBusinesses = businesses.Count;
            var successCount = 0;
            var failureCount = 0;
            var totalInvoicesSynced = 0;

            // Process businesses in batches
            for (int i = 0; i < totalBusinesses; i += _batchSize)
            {
                var batch = businesses.Skip(i).Take(_batchSize).ToList();

                _logger.LogInformation(
                    "Processing batch {BatchNumber} of {TotalBatches} ({Count} businesses)",
                    (i / _batchSize) + 1,
                    (int)Math.Ceiling((double)totalBusinesses / _batchSize),
                    batch.Count);

                // Process each business in the batch
                foreach (var business in batch)
                {
                    try
                    {
                        // Skip if TIN is invalid
                        if (string.IsNullOrWhiteSpace(business.TIN) || business.TIN.Length < 7)
                        {
                            _logger.LogWarning(
                                "Skipping business {BusinessId} ({Name}) - Invalid TIN: {TIN}",
                                business.Id, business.Name, business.TIN);
                            continue;
                        }

                        _logger.LogInformation(
                            "Syncing received invoices for business {BusinessId} ({Name}) with TIN {TIN}",
                            business.Id, business.Name, business.TIN);

                        var result = await SyncBusinessInvoicesAsync(
                            business.Id,
                            startDateStr,
                            endDateStr,
                            cancellationToken);

                        if (result.Success)
                        {
                            successCount++;
                            totalInvoicesSynced += result.InvoicesSynced;

                            _logger.LogInformation(
                                "Successfully synced {Count} invoices for business {BusinessId} ({Name})",
                                result.InvoicesSynced, business.Id, business.Name);
                        }
                        else
                        {
                            failureCount++;

                            _logger.LogWarning(
                                "Failed to sync invoices for business {BusinessId} ({Name}): {Message}",
                                business.Id, business.Name, result.Message);

                            if (result.Errors.Any())
                            {
                                foreach (var error in result.Errors)
                                {
                                    _logger.LogWarning("  - {Error}", error);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;

                        _logger.LogError(ex,
                            "Error syncing invoices for business {BusinessId} ({Name}): {Message}",
                            business.Id, business.Name, ex.Message);
                    }
                }

                // Delay between batches to avoid overwhelming the system
                if (i + _batchSize < totalBusinesses)
                {
                    _logger.LogInformation(
                        "Waiting {Delay}ms before processing next batch",
                        _delayBetweenBatchesMs);

                    await Task.Delay(_delayBetweenBatchesMs, cancellationToken);
                }
            }

            _logger.LogInformation(
                "Sync completed. Total Businesses: {Total}, Success: {Success}, Failures: {Failures}, Total Invoices Synced: {Invoices}",
                totalBusinesses, successCount, failureCount, totalInvoicesSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving businesses for sync: {Message}",
                ex.Message);
        }
    }

    /// <summary>
    /// Syncs received invoices for a single business using MediatR
    /// </summary>
    private async Task<SyncReceivedInvoicesResult> SyncBusinessInvoicesAsync(
        Guid businessId,
        string startDate,
        string endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create a new scope for this operation to ensure clean DbContext
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new SyncReceivedInvoicesCommand
            {
                BusinessId = businessId,
                StartDate = startDate,
                EndDate = endDate
            };

            return await mediator.Send(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error executing sync command for business {BusinessId}: {Message}",
                businessId, ex.Message);

            return new SyncReceivedInvoicesResult
            {
                Success = false,
                Message = $"Error executing sync: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received Invoices Sync Background Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
