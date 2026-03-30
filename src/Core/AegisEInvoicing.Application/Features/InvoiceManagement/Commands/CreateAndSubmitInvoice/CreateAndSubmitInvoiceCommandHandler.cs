using AegisEInvoicing.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;

/// <summary>
/// Handles the consolidated create and submit invoice operation.
/// Uses InvoiceSubmissionOrchestrator for optimal performance (single transaction, no MediatR overhead)
/// </summary>
public class CreateAndSubmitInvoiceCommandHandler(
    InvoiceSubmissionOrchestrator orchestrator,
    ILogger<CreateAndSubmitInvoiceCommandHandler> logger) : IRequestHandler<CreateAndSubmitInvoiceCommand, CreateAndSubmitInvoiceResult>
{
    private readonly InvoiceSubmissionOrchestrator _orchestrator = orchestrator;
    private readonly ILogger<CreateAndSubmitInvoiceCommandHandler> _logger = logger;

    public async Task<CreateAndSubmitInvoiceResult> Handle(
        CreateAndSubmitInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting consolidated invoice submission for BusinessId: {BusinessId}",
            request.InvoiceData.BusinessId);

        try
        {
            // Delegate to orchestrator - single transaction, no MediatR overhead
            var result = await _orchestrator.SubmitInvoiceAsync(request.InvoiceData, cancellationToken);

            stopwatch.Stop();
            result.Pipeline.TotalExecutionTime = stopwatch.Elapsed;

            _logger.LogInformation(
                "Consolidated submission completed in {ElapsedMs}ms. Success: {Success}, Status: {Status}",
                stopwatch.ElapsedMilliseconds, result.Success, result.CurrentStatus);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error in consolidated invoice submission");

            return new CreateAndSubmitInvoiceResult
            {
                Success = false,
                Message = "An unexpected error occurred during invoice submission",
                ErrorDetails = ex.Message,
                StatusCodes = 500,
                Pipeline = { TotalExecutionTime = stopwatch.Elapsed }
            };
        }
    }
}
