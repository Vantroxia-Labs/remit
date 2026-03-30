using AegisEInvoicing.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitBulkInvoice;

/// <summary>
/// Handles bulk creation and submission of invoices using the orchestrator for performance
/// </summary>
public class CreateAndSubmitBulkInvoiceCommandHandler : IRequestHandler<CreateAndSubmitBulkInvoiceCommand, CreateAndSubmitBulkInvoiceResult>
{
    private readonly InvoiceSubmissionOrchestrator _orchestrator;
    private readonly ILogger<CreateAndSubmitBulkInvoiceCommandHandler> _logger;

    public CreateAndSubmitBulkInvoiceCommandHandler(
        InvoiceSubmissionOrchestrator orchestrator,
        ILogger<CreateAndSubmitBulkInvoiceCommandHandler> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<CreateAndSubmitBulkInvoiceResult> Handle(
        CreateAndSubmitBulkInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CreateAndSubmitBulkInvoiceResult
        {
            TotalProcessed = request.Invoices.Count
        };

        _logger.LogInformation(
            "Starting bulk invoice processing for {Count} invoices",
            request.Invoices.Count);

        if (request.Invoices.Count == 0)
        {
            result.Success = false;
            result.Message = "No invoices provided for processing";
            result.StatusCodes = 400;
            return result;
        }

        try
        {
            // Process each invoice sequentially using the orchestrator
            for (int i = 0; i < request.Invoices.Count; i++)
            {
                var invoiceData = request.Invoices[i];

                _logger.LogInformation(
                    "Processing invoice {Current}/{Total} for business {BusinessId}",
                    i + 1, request.Invoices.Count, invoiceData.BusinessId);

                try
                {
                    // Use orchestrator for each invoice - FAST processing
                    var singleResult = await _orchestrator.SubmitInvoiceAsync(invoiceData, cancellationToken);

                    // Add to results
                    result.Results.Add(singleResult);

                    if (singleResult.Success)
                    {
                        result.SuccessCount++;
                        _logger.LogInformation(
                            "Invoice {Current}/{Total} processed successfully. InvoiceId: {InvoiceId}, IRN: {IRN}",
                            i + 1, request.Invoices.Count, singleResult.InvoiceId, singleResult.IRN);
                    }
                    else
                    {
                        result.FailedCount++;

                        // Add to error summary
                        result.Errors.Add(new BulkProcessingError
                        {
                            InvoiceIndex = i,
                            InvoiceNumber = invoiceData.InvoiceNumber,
                            IRN = singleResult.IRN,
                            ErrorMessage = singleResult.Message,
                            FailedAt = singleResult.FailedAt,
                            ErrorDetails = singleResult.ErrorDetails
                        });

                        _logger.LogWarning(
                            "Invoice {Current}/{Total} failed at {FailedAt} step. Message: {Message}",
                            i + 1, request.Invoices.Count, singleResult.FailedAt, singleResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;

                    // Add to error summary
                    result.Errors.Add(new BulkProcessingError
                    {
                        InvoiceIndex = i,
                        InvoiceNumber = invoiceData.InvoiceNumber,
                        ErrorMessage = $"Unexpected error: {ex.Message}",
                        FailedAt = "unknown",
                        ErrorDetails = ex.ToString()
                    });

                    _logger.LogError(ex,
                        "Unexpected error processing invoice {Current}/{Total} for business {BusinessId}",
                        i + 1, request.Invoices.Count, invoiceData.BusinessId);
                }

                // Log progress every 10 invoices
                if ((i + 1) % 10 == 0)
                {
                    _logger.LogInformation(
                        "Bulk processing progress: {Processed}/{Total} invoices processed. Success: {Success}, Failed: {Failed}",
                        i + 1, request.Invoices.Count, result.SuccessCount, result.FailedCount);
                }
            }

            stopwatch.Stop();
            result.TotalExecutionTime = stopwatch.Elapsed;

            // Determine overall success
            result.Success = result.FailedCount == 0;

            if (result.Success)
            {
                result.Message = $"All {result.TotalProcessed} invoices created and submitted successfully";
                result.StatusCodes = 200;
            }
            else if (result.SuccessCount > 0)
            {
                result.Message = $"Bulk processing completed: {result.SuccessCount} succeeded, {result.FailedCount} failed out of {result.TotalProcessed} total";
                result.StatusCodes = 207; // Multi-Status
            }
            else
            {
                result.Message = $"All {result.TotalProcessed} invoices failed to process";
                result.StatusCodes = 400;
            }

            _logger.LogInformation(
                "Bulk invoice processing completed in {ElapsedMs}ms. Total: {Total}, Success: {Success}, Failed: {Failed}",
                stopwatch.ElapsedMilliseconds, result.TotalProcessed, result.SuccessCount, result.FailedCount);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during bulk invoice processing");

            result.Success = false;
            result.Message = "An unexpected error occurred during bulk invoice processing";
            result.StatusCodes = 500;
            result.TotalExecutionTime = stopwatch.Elapsed;

            return result;
        }
    }
}
