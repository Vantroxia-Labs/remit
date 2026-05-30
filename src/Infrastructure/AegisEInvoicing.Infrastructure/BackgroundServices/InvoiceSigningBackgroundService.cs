using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to process validated invoices and submit them to FIRS for signing
/// </summary>
public sealed class InvoiceSigningBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvoiceSigningBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 20;
    private const int ConcurrentBatchSize = 10;

    public InvoiceSigningBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<InvoiceSigningBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Signing Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessValidatedInvoicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing validated invoices for signing");
            }

            try
            {
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // Service is stopping
            }
        }

        _logger.LogInformation("Invoice Signing Background Service stopped");
    }

    private async Task ProcessValidatedInvoicesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var appProviderRouter = scope.ServiceProvider.GetRequiredService<IAppProviderRouter>();

        // Get all validated invoices that need signing
        var validatedInvoices = await dbContext.Invoices
            .Include(i => i.Business)
            .Include(i => i.Party)
            .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
            .Where(i => i.InvoiceStatus == InvoiceStatus.VALIDATED)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (!validatedInvoices.Any())
        {
            _logger.LogDebug("No validated invoices found for signing");
            return;
        }

        _logger.LogInformation("Processing {Count} validated invoices for signing", validatedInvoices.Count);

        // Group invoices by business to handle different credentials efficiently
        var invoicesByBusiness = validatedInvoices.GroupBy(i => i.Business);

        foreach (var businessGroup in invoicesByBusiness)
        {
            await ProcessBusinessInvoices(businessGroup.Key, businessGroup.ToList(), dbContext, appProviderRouter, cancellationToken);
        }

        _logger.LogInformation("Completed processing {Count} validated invoices", validatedInvoices.Count);
    }

    private async Task ProcessBusinessInvoices(
        Business business,
        List<Invoice> invoices,
        IApplicationDbContext dbContext,
        IAppProviderRouter appProviderRouter,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get configured provider adapter for this business
            var provider = await appProviderRouter.GetProviderAsync(business.Id, cancellationToken);

            // Process invoices in smaller concurrent batches
            var batches = invoices.Chunk(ConcurrentBatchSize);

            foreach (var batch in batches)
            {
                await ProcessInvoiceBatch(batch, provider, dbContext, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider adapter for business {BusinessId}", business.Id);
            await MarkInvoicesAsFailed(invoices, "SIGNING FAILED: Unable to configure APP provider", dbContext, cancellationToken);
        }
    }

    private async Task ProcessInvoiceBatch(
        IEnumerable<Invoice> invoices,
        IAccessPointProviderClient provider,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Use execution strategy to handle retries with transactions
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Process invoices concurrently within the batch
                var signingTasks = invoices.Select(invoice =>
                    SignSingleInvoice(invoice, provider, cancellationToken));

                await Task.WhenAll(signingTasks);

                // Add approval histories for all invoices in the batch
                foreach (var invoice in invoices)
                {
                    dbContext.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, invoice.FIRSSubmissionResponseMessage!));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully processed batch of {Count} invoices using {Provider}",
                    invoices.Count(), provider.DisplayName);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                var invoiceIds = string.Join(",", invoices.Select(i => i.Id));
                _logger.LogError(ex, "Failed to process invoice batch for business {BusinessId}: {InvoiceIds}", invoices.FirstOrDefault()?.BusinessId, invoiceIds);

                // Mark all invoices in the batch as failed
                foreach (var invoice in invoices)
                {
                    invoice.SetFIRSSubmissionResponseMessage("SIGNING FAILED: Batch processing error");
                    invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
                    dbContext.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.SIGNINGFAILED, ResponseMessages.INVOICE_SIGNING_FAILED));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        });
    }

    private async Task SignSingleInvoice(
        Invoice invoice,
        IAccessPointProviderClient provider,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Signing invoice {InvoiceId} with IRN {IRN} for business {BusinessId}",
                invoice.Id, invoice.Irn?.Value, invoice.BusinessId);

            var result = await provider.SignInvoiceAsync(invoice, cancellationToken);

            if (result.IsSuccess)
            {
                invoice.UpdateStatus(InvoiceStatus.SIGNED);
                invoice.SetFIRSSubmissionId(invoice.Irn?.Value ?? Guid.NewGuid().ToString());
                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_SIGNING_SUCCESSFUL);
                invoice.SetSubmittedToFIRSAt(DateTimeOffset.UtcNow);

                _logger.LogInformation("Invoice {InvoiceId} successfully signed by FIRS", invoice.Id);
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} signing failed: {ErrorCode} - {ErrorMessage}",
                    invoice.Id, result.ErrorCode, result.ErrorMessage);
                invoice.SetFIRSSubmissionResponseMessage(result.ErrorMessage ?? "Signing failed");
                invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing invoice {InvoiceId}", invoice.Id);
            invoice.SetFIRSSubmissionResponseMessage(ex.InnerException?.Message ?? ex.Message);
            invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
        }
    }

    private async Task MarkInvoicesAsFailed(
        IEnumerable<Invoice> invoices,
        string message,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Use execution strategy to handle retries with transactions
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var invoice in invoices)
                {
                    invoice.SetFIRSSubmissionResponseMessage(message);
                    invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
                    dbContext.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.SIGNINGFAILED, message));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogWarning("Marked {Count} invoices as failed: {Message}", invoices.Count(), message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to mark invoices as failed");
                throw;
            }
        });
    }
}