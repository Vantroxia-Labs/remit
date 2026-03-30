using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoice;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoicesBulk;

/// <summary>
/// Handles bulk submission of existing invoices through the pipeline
/// </summary>
public class SubmitExistingInvoicesBulkCommandHandler : IRequestHandler<SubmitExistingInvoicesBulkCommand, CreateAndSubmitBulkInvoiceResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubmitExistingInvoicesBulkCommandHandler> _logger;

    public SubmitExistingInvoicesBulkCommandHandler(
        IMediator mediator,
        ILogger<SubmitExistingInvoicesBulkCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<CreateAndSubmitBulkInvoiceResult> Handle(
        SubmitExistingInvoicesBulkCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CreateAndSubmitBulkInvoiceResult
        {
            TotalProcessed = request.InvoiceIds.Count
        };

        _logger.LogInformation(
            "Starting bulk submission for {Count} existing invoices",
            request.InvoiceIds.Count);

        if (request.InvoiceIds.Count == 0)
        {
            result.Success = false;
            result.Message = "No invoice IDs provided for processing";
            result.StatusCodes = 400;
            return result;
        }

        try
        {
            // Process each invoice sequentially
            for (int i = 0; i < request.InvoiceIds.Count; i++)
            {
                var invoiceId = request.InvoiceIds[i];

                _logger.LogInformation(
                    "Processing invoice {Current}/{Total}: {InvoiceId}",
                    i + 1, request.InvoiceIds.Count, invoiceId);

                try
                {
                    // Create command for single invoice submission
                    var singleCommand = new SubmitExistingInvoiceCommand
                    {
                        InvoiceId = invoiceId
                    };

                    // Execute the pipeline for this invoice
                    var singleResult = await _mediator.Send(singleCommand, cancellationToken);

                    // Add to results
                    result.Results.Add(singleResult);

                    if (singleResult.Success)
                    {
                        result.SuccessCount++;
                        _logger.LogInformation(
                            "Invoice {Current}/{Total} processed successfully. InvoiceId: {InvoiceId}, IRN: {IRN}",
                            i + 1, request.InvoiceIds.Count, singleResult.InvoiceId, singleResult.IRN);
                    }
                    else
                    {
                        result.FailedCount++;

                        // Add to error summary
                        result.Errors.Add(new BulkProcessingError
                        {
                            InvoiceIndex = i,
                            InvoiceNumber = invoiceId.ToString(),
                            IRN = singleResult.IRN,
                            ErrorMessage = singleResult.Message,
                            FailedAt = singleResult.FailedAt,
                            ErrorDetails = singleResult.ErrorDetails
                        });

                        _logger.LogWarning(
                            "Invoice {Current}/{Total} failed at {FailedAt} step. InvoiceId: {InvoiceId}, Message: {Message}",
                            i + 1, request.InvoiceIds.Count, singleResult.FailedAt, invoiceId, singleResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;

                    // Add to error summary
                    result.Errors.Add(new BulkProcessingError
                    {
                        InvoiceIndex = i,
                        InvoiceNumber = invoiceId.ToString(),
                        ErrorMessage = $"Unexpected error: {ex.Message}",
                        FailedAt = "unknown",
                        ErrorDetails = ex.ToString()
                    });

                    _logger.LogError(ex,
                        "Unexpected error processing invoice {Current}/{Total}: {InvoiceId}",
                        i + 1, request.InvoiceIds.Count, invoiceId);

                    // Add a failed result placeholder
                    result.Results.Add(new CreateAndSubmitInvoiceResult
                    {
                        Success = false,
                        InvoiceId = invoiceId,
                        Message = $"Unexpected error: {ex.Message}",
                        ErrorDetails = ex.ToString(),
                        FailedAt = "unknown",
                        StatusCodes = 500
                    });
                }

                // Log progress every 10 invoices
                if ((i + 1) % 10 == 0)
                {
                    _logger.LogInformation(
                        "Bulk processing progress: {Processed}/{Total} invoices processed. Success: {Success}, Failed: {Failed}",
                        i + 1, request.InvoiceIds.Count, result.SuccessCount, result.FailedCount);
                }
            }

            stopwatch.Stop();
            result.TotalExecutionTime = stopwatch.Elapsed;

            // Determine overall success
            result.Success = result.FailedCount == 0;

            if (result.Success)
            {
                result.Message = $"All {result.TotalProcessed} invoices submitted successfully";
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
                "Bulk submission completed in {ElapsedMs}ms. Total: {Total}, Success: {Success}, Failed: {Failed}",
                stopwatch.ElapsedMilliseconds, result.TotalProcessed, result.SuccessCount, result.FailedCount);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during bulk invoice submission");

            result.Success = false;
            result.Message = "An unexpected error occurred during bulk invoice submission";
            result.StatusCodes = 500;
            result.TotalExecutionTime = stopwatch.Elapsed;

            return result;
        }
    }
}
