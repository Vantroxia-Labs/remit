using AegisEInvoicing.Application.Common.Extensions;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to process approved invoices and submit them to FIRS for validation
/// </summary>
public sealed class InvoiceValidationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvoiceValidationBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 20;
    private const int ConcurrentBatchSize = 10;

    public InvoiceValidationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<InvoiceValidationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Validation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessApprovedInvoicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approved invoices for validation");
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

        _logger.LogInformation("Invoice Validation Background Service stopped");
    }

    private async Task ProcessApprovedInvoicesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var firsClient = scope.ServiceProvider.GetRequiredService<IFIRSHttpClient>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        // Get all approved invoices that need validation
        var approvedInvoices = await dbContext.Invoices
            .Include(i => i.Business)
            .Include(i => i.Party)
            .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
                .ThenInclude(bi => bi!.ItemCategory)
            .Where(i => i.InvoiceStatus == InvoiceStatus.APPROVED)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (!approvedInvoices.Any())
        {
            _logger.LogDebug("No approved invoices found for validation");
            return;
        }

        _logger.LogInformation("Processing {Count} approved invoices for validation", approvedInvoices.Count);

        // Group invoices by business to handle different credentials efficiently
        var invoicesByBusiness = approvedInvoices.GroupBy(i => i.Business);

        foreach (var businessGroup in invoicesByBusiness)
        {
            await ProcessBusinessInvoices(businessGroup.Key, businessGroup.ToList(), dbContext, firsClient, encryptionService, cancellationToken);
        }

        _logger.LogInformation("Completed processing {Count} approved invoices", approvedInvoices.Count);
    }

    private async Task ProcessBusinessInvoices(
        Business business,
        List<Invoice> invoices,
        IApplicationDbContext dbContext,
        IFIRSHttpClient firsClient,
        IEncryptionService encryptionService,
        CancellationToken cancellationToken)
    {
        // Check if business has FIRS credentials
        if (string.IsNullOrEmpty(business.FIRSApiKey) || string.IsNullOrEmpty(business.FIRSClientSecret))
        {
            await MarkInvoicesAsFailed(invoices, "VALIDATION FAILED: FIRS credentials not configured", dbContext, cancellationToken);
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
            await MarkInvoicesAsFailed(invoices, "VALIDATION FAILED: Unable to decrypt FIRS credentials", dbContext, cancellationToken);
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
        IFIRSHttpClient firsClient,
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
                var validationTasks = invoices.Select(invoice =>
                    ValidateSingleInvoice(invoice, firsBusinessId, apiKey, clientSecret, firsClient, cancellationToken));

                await Task.WhenAll(validationTasks);

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
                    invoice.SetFIRSSubmissionResponseMessage("VALIDATION FAILED: Batch processing error");
                    invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
                    dbContext.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.VALIDATIONFAILED, ResponseMessages.INVOICE_VALIDATION_FAILED));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        });
    }

    private async Task ValidateSingleInvoice(
        Invoice invoice,
        Guid firsBusinessId,
        string apiKey,
        string clientSecret,
        IFIRSHttpClient firsClient,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Validating invoice {InvoiceId} with IRN {IRN} for business {BusinessId}",
                invoice.Id, invoice.Irn?.Value, invoice.BusinessId);

            var validationRequest = BuildValidationRequest(invoice, firsBusinessId);
            var response = await firsClient.ValidateInvoiceDataAsync(validationRequest, apiKey, clientSecret, cancellationToken);

            if (IsValidationSuccessful(response))
            {
                invoice.SetFIRSSubmissionResponseMessage("Invoice Validated");
                invoice.UpdateStatus(InvoiceStatus.VALIDATED);
                _logger.LogInformation("Invoice {InvoiceId} successfully validated by FIRS", invoice.Id);
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} validation failed with code: {Code}", invoice.Id, response.Code);
                invoice.SetFIRSSubmissionResponseMessage(response.Error?.PublicMessage ?? "Validation failed");
                invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invoice {InvoiceId}", invoice.Id);
            invoice.SetFIRSSubmissionResponseMessage(ex.InnerException?.Message ?? ex.Message);
            invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
        }
    }

    private static ValidateInvoiceDataRequest BuildValidationRequest(Invoice invoice, Guid firsBusinessId) =>
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
            InvoiceDeliveryPeriod = invoice.DeliveryPeriod.ToInvoiceDeliveryPeriod(),
            AccountingCustomerParty = invoice.Party.ToAccountingCustomerParty(),
            AccountingSupplierParty = invoice.Business.ToAccountingSupplierParty(),
            PaymentMeans = invoice.PaymentMeans!.ToPaymentMeans(invoice.IssueDate.AddDays(7)),
            PaymentTermsNote = invoice.PaymentTerms,
            AllowanceCharge = invoice.InvoiceLine.ToList().ToAllowanceCharge(),
            TaxTotal = invoice.InvoiceLine.ToList().ToTaxTotal(),
            LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToLegalMonetaryTotal(),
            InvoiceLine = invoice.InvoiceLine.ToList().ToInvoiceLine(invoice.Currency.Code)
        };

    private static bool IsValidationSuccessful(dynamic response) =>
        response.Code == 200 || response.Code == 0 || (response.Data?.Ok ?? false);

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
                    invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
                    dbContext.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.VALIDATIONFAILED, message));
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