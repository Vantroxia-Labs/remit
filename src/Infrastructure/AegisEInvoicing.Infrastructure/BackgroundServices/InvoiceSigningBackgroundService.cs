using AegisEInvoicing.Application.Common.Extensions;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Interswitch;
using AegisEInvoicing.Interswitch.Interfaces;
using AegisEInvoicing.Interswitch.Models.Requests.SignInvoice;
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
        var interswitchClient = scope.ServiceProvider.GetRequiredService<IInterswitchHttpClient>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        // Get all validated invoices that need signing
        var validatedInvoices = await dbContext.Invoices
            .Include(i => i.Business)
            .Include(i => i.Party)
            .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
                .ThenInclude(bi => bi.ItemCategory)
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
            await ProcessBusinessInvoices(businessGroup.Key, businessGroup.ToList(), dbContext, interswitchClient, encryptionService, cancellationToken);
        }

        _logger.LogInformation("Completed processing {Count} validated invoices", validatedInvoices.Count);
    }

    private async Task ProcessBusinessInvoices(
        Business business,
        List<Invoice> invoices,
        IApplicationDbContext dbContext,
        IInterswitchHttpClient firsClient,
        IEncryptionService encryptionService,
        CancellationToken cancellationToken)
    {
        // Check if business has FIRS credentials
        if (string.IsNullOrEmpty(business.FIRSApiKey) || string.IsNullOrEmpty(business.FIRSClientSecret))
        {
            await MarkInvoicesAsFailed(invoices, "SIGNING FAILED: FIRS credentials not configured", dbContext, cancellationToken);
            return;
        }

        try
        {
            // Decrypt credentials once per business
            var (apiKey, clientSecret) = await DecryptCredentials(business.FIRSApiKey, business.FIRSClientSecret, encryptionService);

            // Process invoices in smaller concurrent batches
            var batches = invoices.Chunk(ConcurrentBatchSize);

            foreach (var batch in batches)
            {
                await ProcessInvoiceBatch(batch, business.FIRSBusinessId, apiKey, clientSecret, dbContext, firsClient, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt credentials for business {BusinessId}", business.Id);
            await MarkInvoicesAsFailed(invoices, "SIGNING FAILED: Unable to decrypt FIRS credentials", dbContext, cancellationToken);
        }
    }

    private async Task<(string apiKey, string clientSecret)> DecryptCredentials(
        string encryptedApiKey,
        string encryptedClientSecret,
        IEncryptionService encryptionService)
    {
        var decryptTasks = new[]
        {
            encryptionService.DecryptAsync(encryptedApiKey),
            encryptionService.DecryptAsync(encryptedClientSecret)
        };

        var results = await Task.WhenAll(decryptTasks);
        return (results[0], results[1]);
    }

    private async Task ProcessInvoiceBatch(
        IEnumerable<Invoice> invoices,
        Guid firsBusinessId,
        string apiKey,
        string clientSecret,
        IApplicationDbContext dbContext,
        IInterswitchHttpClient firsClient,
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
                    SignSingleInvoice(invoice, firsBusinessId, apiKey, clientSecret, firsClient, cancellationToken));

                await Task.WhenAll(signingTasks);

                // Add approval histories for all invoices in the batch
                foreach (var invoice in invoices)
                {
                    dbContext.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, invoice.FIRSSubmissionResponseMessage!));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully processed batch of {Count} invoices for business {BusinessId}",
                    invoices.Count(), firsBusinessId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                var invoiceIds = string.Join(",", invoices.Select(i => i.Id));
                _logger.LogError(ex, "Failed to process invoice batch for business {BusinessId}: {InvoiceIds}", firsBusinessId, invoiceIds);

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
        Guid firsBusinessId,
        string apiKey,
        string clientSecret,
        IInterswitchHttpClient firsClient,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Signing invoice {InvoiceId} with IRN {IRN} for business {BusinessId}",
                invoice.Id, invoice.Irn?.Value, invoice.BusinessId);

            var signingRequest = BuildSigningRequest(invoice, firsBusinessId);
            var response = await firsClient.SignInvoiceAsync(signingRequest, cancellationToken);

            if (IsSigningSuccessful(response))
            {
                invoice.UpdateStatus(InvoiceStatus.SIGNED);
                invoice.SetFIRSSubmissionId(invoice.Irn?.Value ?? Guid.NewGuid().ToString());
                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_SIGNING_SUCCESSFUL);
                invoice.SetSubmittedToFIRSAt(DateTimeOffset.UtcNow);

                _logger.LogInformation("Invoice {InvoiceId} successfully signed by FIRS", invoice.Id);
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} signing failed with code: {Code}", invoice.Id, response.Code);
                invoice.SetFIRSSubmissionResponseMessage(response.Error?.PublicMessage ?? "Signing failed");
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

    private static SignInvoiceRequest BuildSigningRequest(Invoice invoice, Guid firsBusinessId) =>
        new()
        {
            BusinessId = firsBusinessId.ToString(),
            Irn = invoice.Irn?.Value ?? string.Empty,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            IssueTime = invoice.IssueTime,
            InvoiceTypeCode = invoice.InvoiceType.Code.ToString(),
            PaymentStatus = invoice.PaymentStatus.GetDisplayName(),
            Note = invoice.Note,
            DocumentCurrencyCode = invoice.Currency.Code,
            TaxCurrencyCode = invoice.Currency.Code,
            InvoiceDeliveryPeriod = invoice.DeliveryPeriod.ToSigningInvoiceDeliveryPeriod(),
            AccountingCustomerParty = invoice.Party.ToSigningAccountingCustomerParty(),
            AccountingSupplierParty = invoice.Business.ToSigningAccountingSupplierParty(),
            PaymentMeans = invoice.PaymentMeans!.ToSigningPaymentMeans(invoice.IssueDate.AddDays(7)),
            PaymentTermsNote = invoice.PaymentTerms,
            AllowanceCharge = invoice.InvoiceLine.ToList().ToSigningAllowanceCharge(),
            TaxTotal = invoice.InvoiceLine.ToList().ToSigningTaxTotal(),
            LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToSigningLegalMonetaryTotal(),
            InvoiceLine = invoice.InvoiceLine.ToList().ToSigningInvoiceLine(invoice.Currency.Code)
        };

    private static bool IsSigningSuccessful(dynamic response) =>
        (response.Data!.Data!.Ok ?? false);

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