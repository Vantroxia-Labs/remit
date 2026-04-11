using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoice;

/// <summary>
/// Handles the submission of an existing invoice through the pipeline:
/// Validate ? Sign ? Transmit
/// </summary>

public class SubmitExistingInvoiceCommandHandler : IRequestHandler<SubmitExistingInvoiceCommand, CreateAndSubmitInvoiceResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubmitExistingInvoiceCommandHandler> _logger;
    private readonly IApplicationDbContext _context;

    public SubmitExistingInvoiceCommandHandler(
        IMediator mediator,
        ILogger<SubmitExistingInvoiceCommandHandler> logger,
        IApplicationDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _context = context;
    }

    public async Task<CreateAndSubmitInvoiceResult> Handle(
        SubmitExistingInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CreateAndSubmitInvoiceResult();

        _logger.LogInformation(
            "Starting submission pipeline for existing invoice {InvoiceId}",
            request.InvoiceId);

        try
        {
            // First, verify the invoice exists and get its IRN
            var invoice = await _context.Invoices
                .Where(i => i.Id == request.InvoiceId)
                .Select(i => new { i.Id, i.Irn, i.InvoiceKind })
                .FirstOrDefaultAsync(cancellationToken);

            if (invoice == null)
            {
                result.Success = false;
                result.Message = "Invoice not found";
                result.StatusCodes = 404;
                result.FailedAt = "validate";
                return result;
            }

            if (invoice.InvoiceKind == InvoiceKind.B2C)
            {
                result.Success = false;
                result.Message = ResponseMessages.B2C_INVOICE_CANNOT_BE_TRANSMITTED;
                result.StatusCodes = 400;
                result.FailedAt = "transmit";
                result.InvoiceId = invoice.Id;
                result.IRN = invoice.Irn?.Value ?? string.Empty;
                return result;
            }

            result.InvoiceId = invoice.Id;
            result.IRN = invoice.Irn.Value;

            // Execute the pipeline (Validate ? Sign ? Transmit)
            result = await ExecutePipeline(request.InvoiceId, invoice.Irn.Value, cancellationToken);

            stopwatch.Stop();
            result.Pipeline.TotalExecutionTime = stopwatch.Elapsed;

            _logger.LogInformation(
                "Submission pipeline completed in {ElapsedMs}ms. Success: {Success}, Status: {Status}, FailedAt: {FailedAt}",
                stopwatch.ElapsedMilliseconds,
                result.Success,
                result.CurrentStatus,
                result.FailedAt ?? "None");

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unexpected error in submission pipeline for invoice {InvoiceId}",
                request.InvoiceId);

            result.Success = false;
            result.Message = "An unexpected error occurred during invoice submission";
            result.ErrorDetails = ex.Message;
            result.StatusCodes = 500;
            result.Pipeline.TotalExecutionTime = stopwatch.Elapsed;

            return result;
        }
    }

    /// <summary>
    /// Executes the pipeline: Validate ? Sign ? Transmit
    /// </summary>
    private async Task<CreateAndSubmitInvoiceResult> ExecutePipeline(
        Guid invoiceId,
        string irn,
        CancellationToken cancellationToken)
    {
        var result = new CreateAndSubmitInvoiceResult
        {
            InvoiceId = invoiceId,
            IRN = irn
        };

        // STEP 1: VALIDATE INVOICE
        _logger.LogInformation("Pipeline Step 1/3: Validating invoice {InvoiceId}", invoiceId);
        result.Pipeline.Validate = await ExecuteValidateStep(invoiceId, cancellationToken);

        if (result.Pipeline.Validate.Status == "SUCCESS")
        {
            result.CurrentStatus = InvoiceStatus.VALIDATED;
            _logger.LogInformation("Pipeline Step 1/3: Invoice validated successfully");
        }
        else
        {
            _logger.LogWarning(
                "Pipeline Step 1/3: Invoice validation failed. {Message}. Continuing to next step...",
                result.Pipeline.Validate.Message);

            result.FailedAt = "validate";
            result.CurrentStatus = InvoiceStatus.VALIDATIONFAILED;
        }

        // STEP 2: SIGN INVOICE
        _logger.LogInformation("Pipeline Step 2/3: Signing invoice {InvoiceId}", invoiceId);
        result.Pipeline.Sign = await ExecuteSignStep(invoiceId, cancellationToken);

        if (result.Pipeline.Sign.Status == "SUCCESS")
        {
            result.CurrentStatus = InvoiceStatus.SIGNED;
            _logger.LogInformation("Pipeline Step 2/3: Invoice signed successfully");
        }
        else
        {
            _logger.LogWarning(
                "Pipeline Step 2/3: Invoice signing failed. {Message}. Continuing to next step...",
                result.Pipeline.Sign.Message);

            result.FailedAt ??= "sign";
            result.CurrentStatus = InvoiceStatus.SIGNINGFAILED;
        }

        // STEP 3: TRANSMIT INVOICE
        _logger.LogInformation("Pipeline Step 3/3: Transmitting invoice {InvoiceId}", invoiceId);
        result.Pipeline.Transmit = await ExecuteTransmitStep(invoiceId, cancellationToken);

        if (result.Pipeline.Transmit.Status == "SUCCESS")
        {
            result.CurrentStatus = InvoiceStatus.TRANSMITTED;
            _logger.LogInformation("Pipeline Step 3/3: Invoice transmitted successfully");
        }
        else
        {
            _logger.LogWarning(
                "Pipeline Step 3/3: Invoice transmission failed. {Message}",
                result.Pipeline.Transmit.Message);

            result.FailedAt ??= "transmit";
            result.CurrentStatus = InvoiceStatus.TRANSMISSIONFAILED;
        }

        // Determine overall success
        result.Success = result.Pipeline.Validate?.Status == "SUCCESS" &&
                         result.Pipeline.Sign?.Status == "SUCCESS" &&
                         result.Pipeline.Transmit?.Status == "SUCCESS";

        if (result.Success)
        {
            result.Message = "Invoice submitted successfully through the entire pipeline";
            result.StatusCodes = 200;
        }
        else
        {
            result.Message = $"Invoice submission failed at {result.FailedAt} step. Current status: {result.CurrentStatus}";
            result.StatusCodes = 207; // Multi-Status
        }

        return result;
    }

    private async Task<PipelineStepResult> ExecuteValidateStep(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var validateCommand = new ValidateInvoiceCommand(invoiceId);
            var validateResult = await _mediator.Send(validateCommand, cancellationToken);

            if (validateResult.IsSuccess)
            {
                return PipelineStepResult.Success(
                    validateResult.Message ?? "Invoice validated successfully",
                    statusCode: validateResult.StatusCodes);
            }
            else
            {
                return PipelineStepResult.Failed(
                    validateResult.Message ?? "Invoice validation failed",
                    statusCode: validateResult.StatusCodes,
                    errorDetails: validateResult.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during invoice validation step for invoice {InvoiceId}", invoiceId);
            return PipelineStepResult.Failed(
                "Invoice validation failed due to an unexpected error",
                statusCode: 500,
                errorDetails: ex.Message);
        }
    }

    private async Task<PipelineStepResult> ExecuteSignStep(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var signCommand = new SignInvoiceCommand(invoiceId);
            var signResult = await _mediator.Send(signCommand, cancellationToken);

            if (signResult.IsSuccess)
            {
                return PipelineStepResult.Success(
                    signResult.Message ?? "Invoice signed successfully",
                    statusCode: signResult.StatusCodes);
            }
            else
            {
                return PipelineStepResult.Failed(
                    signResult.Message ?? "Invoice signing failed",
                    statusCode: signResult.StatusCodes,
                    errorDetails: signResult.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during invoice signing step for invoice {InvoiceId}", invoiceId);
            return PipelineStepResult.Failed(
                "Invoice signing failed due to an unexpected error",
                statusCode: 500,
                errorDetails: ex.Message);
        }
    }

    private async Task<PipelineStepResult> ExecuteTransmitStep(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var transmitCommand = new TransmitInvoiceCommand(invoiceId);
            var transmitResult = await _mediator.Send(transmitCommand, cancellationToken);

            if (transmitResult.IsSuccess)
            {
                return PipelineStepResult.Success(
                    transmitResult.Message ?? "Invoice transmitted successfully",
                    statusCode: transmitResult.StatusCodes);
            }
            else
            {
                return PipelineStepResult.Failed(
                    transmitResult.Message ?? "Invoice transmission failed",
                    statusCode: transmitResult.StatusCodes,
                    errorDetails: transmitResult.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during invoice transmission step for invoice {InvoiceId}", invoiceId);
            return PipelineStepResult.Failed(
                "Invoice transmission failed due to an unexpected error",
                statusCode: 500,
                errorDetails: ex.Message);
        }
    }
}
