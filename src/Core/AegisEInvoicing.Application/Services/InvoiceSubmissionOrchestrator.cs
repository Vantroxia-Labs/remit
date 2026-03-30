using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Services;

/// <summary>
/// Orchestrates the complete invoice submission workflow with SMART RETRY/RESUME:
/// - First run: Create then Validate then Sign then Transmit
/// - Retry after VALIDATE fails: Skip Create, Resume at Validate
/// - Retry after SIGN fails: Skip Create and Validate, Resume at Sign
/// - Retry after TRANSMIT fails: Skip Create and Validate and Sign, Resume at Transmit
/// Uses direct handler invocation for maximum performance (no MediatR overhead)
/// </summary>
public class InvoiceSubmissionOrchestrator(
    IApplicationDbContext context,
    CreateFIRSInvoiceCommandHandler createHandler,
    ValidateInvoiceCommandHandler validateHandler,
    SignInvoiceCommandHandler signHandler,
    TransmitInvoiceCommandHandler transmitHandler,
    ILogger<InvoiceSubmissionOrchestrator> logger,
    ITelemetryService? telemetryService = null)
{
    private readonly IApplicationDbContext _context = context;
    private readonly CreateFIRSInvoiceCommandHandler _createHandler = createHandler;
    private readonly ValidateInvoiceCommandHandler _validateHandler = validateHandler;
    private readonly SignInvoiceCommandHandler _signHandler = signHandler;
    private readonly TransmitInvoiceCommandHandler _transmitHandler = transmitHandler;
    private readonly ILogger<InvoiceSubmissionOrchestrator> _logger = logger;
    private readonly ITelemetryService? _telemetryService = telemetryService;

    /// <summary>
    /// Submits a single invoice through the complete pipeline with intelligent resume logic
    /// </summary>
    public async Task<CreateAndSubmitInvoiceResult> SubmitInvoiceAsync(
        CreateFIRSInvoiceCommand createData,
        CancellationToken cancellationToken)
    {
        var result = new CreateAndSubmitInvoiceResult();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting invoice submission orchestration");

            // Check if invoice already exists (retry scenario)
            var existingInvoice = await CheckExistingInvoiceAsync(createData, cancellationToken);

            Guid invoiceId;
            InvoiceStatus currentStatus;

            if (existingInvoice != null)
            {
                // RESUME MODE: Invoice exists, skip creation
                invoiceId = existingInvoice.Value.InvoiceId;
                currentStatus = existingInvoice.Value.Status;
                result.InvoiceId = invoiceId;
                result.IRN = existingInvoice.Value.IRN;
                result.CurrentStatus = currentStatus;
                result.Pipeline.Create = PipelineStepResult.Success("Skipped - invoice already exists", 200);

                _logger.LogInformation(
                    "RESUME MODE: Found existing invoice {InvoiceId} (IRN: {IRN}) with status {Status}. Resuming from failed step.",
                    invoiceId, result.IRN, currentStatus);
            }
            else
            {
                // NEW MODE: Create invoice
                _logger.LogInformation("Pipeline Step 1/4: Creating new invoice");
                var createResult = await _createHandler.Handle(createData, cancellationToken);
                result.Pipeline.Create = MapCreateResult(createResult);

                if (!createResult.Success)
                {
                    result.Success = false;
                    result.FailedAt = "create";
                    result.Message = "Invoice creation failed";
                    result.StatusCodes = 400;
                    result.InvoiceId = Guid.Empty;
                    result.IRN = string.Empty;
                    result.Pipeline.TotalExecutionTime = DateTime.UtcNow - startTime;
                    return result;
                }

                invoiceId = createResult.InvoiceId!.Value;
                currentStatus = InvoiceStatus.APPROVED;
                result.InvoiceId = createResult.InvoiceId;
                result.IRN = createResult.IRN;
                result.CurrentStatus = currentStatus;

                _logger.LogInformation("Invoice created: ID={InvoiceId}, IRN={IRN}", invoiceId, result.IRN);
            }

            // STEP 2: VALIDATE
            if (ShouldExecuteValidation(currentStatus))
            {
                _logger.LogInformation("Pipeline Step 2/4: Validating invoice {InvoiceId}", invoiceId);
                var validateCommand = new ValidateInvoiceCommand(invoiceId);
                var validateResult = await _validateHandler.Handle(validateCommand, cancellationToken);
                result.Pipeline.Validate = MapValidateResult(validateResult);

                if (validateResult.IsSuccess)
                {
                    result.CurrentStatus = InvoiceStatus.VALIDATED;
                    currentStatus = InvoiceStatus.VALIDATED;
                    _logger.LogInformation("Invoice validated successfully");
                }
                else
                {
                    // STOP HERE - Return immediately to allow retry from validation
                    result.Success = false;
                    result.FailedAt = "validate";
                    result.CurrentStatus = InvoiceStatus.VALIDATIONFAILED;
                    result.Message = $"Invoice validation failed: {validateResult.Message}. Retry will resume from validation step.";
                    result.StatusCodes = 207;
                    result.Pipeline.TotalExecutionTime = DateTime.UtcNow - startTime;
                    _logger.LogWarning("Validation failed. Next retry will skip CREATE and resume at VALIDATE.");
                    return result;
                }
            }
            else
            {
                _logger.LogInformation("Skipping validation - already completed (current status: {Status})", currentStatus);
                result.Pipeline.Validate = PipelineStepResult.Success("Skipped - already validated", 200);
                result.CurrentStatus = InvoiceStatus.VALIDATED;
            }

            // STEP 3: SIGN
            if (ShouldExecuteSigning(currentStatus))
            {
                _logger.LogInformation("Pipeline Step 3/4: Signing invoice {InvoiceId}", invoiceId);
                var signCommand = new SignInvoiceCommand(invoiceId);
                var signResult = await _signHandler.Handle(signCommand, cancellationToken);
                result.Pipeline.Sign = MapSignResult(signResult);

                if (signResult.IsSuccess)
                {
                    result.CurrentStatus = InvoiceStatus.SIGNED;
                    currentStatus = InvoiceStatus.SIGNED;
                    _logger.LogInformation("Invoice signed successfully");
                }
                else
                {
                    // STOP HERE - Return immediately to allow retry from signing
                    result.Success = false;
                    result.FailedAt = "sign";
                    result.CurrentStatus = InvoiceStatus.SIGNINGFAILED;
                    result.Message = $"Invoice signing failed: {signResult.Message}. Retry will resume from signing step.";
                    result.StatusCodes = 207;
                    result.Pipeline.TotalExecutionTime = DateTime.UtcNow - startTime;
                    _logger.LogWarning("Signing failed. Next retry will skip CREATE & VALIDATE and resume at SIGN.");
                    return result;
                }
            }
            else
            {
                _logger.LogInformation("Skipping signing - already completed (current status: {Status})", currentStatus);
                result.Pipeline.Sign = PipelineStepResult.Success("Skipped - already signed", 200);
                result.CurrentStatus = InvoiceStatus.SIGNED;
            }

            // STEP 4: TRANSMIT
            if (ShouldExecuteTransmission(currentStatus))
            {
                _logger.LogInformation("Pipeline Step 4/4: Transmitting invoice {InvoiceId}", invoiceId);
                var transmitCommand = new TransmitInvoiceCommand(invoiceId);
                var transmitResult = await _transmitHandler.Handle(transmitCommand, cancellationToken);
                result.Pipeline.Transmit = MapTransmitResult(transmitResult);

                if (transmitResult.IsSuccess)
                {
                    result.CurrentStatus = InvoiceStatus.TRANSMITTED;
                    currentStatus = InvoiceStatus.TRANSMITTED;
                    _logger.LogInformation("Invoice transmitted successfully");
                }
                else
                {
                    // STOP HERE - Return immediately to allow retry from transmission
                    result.Success = false;
                    result.FailedAt = "transmit";
                    result.CurrentStatus = InvoiceStatus.TRANSMISSIONFAILED;
                    result.Message = $"Invoice transmission failed: {transmitResult.Message}. Retry will resume from transmission step.";
                    result.StatusCodes = 207;
                    result.Pipeline.TotalExecutionTime = DateTime.UtcNow - startTime;
                    _logger.LogWarning("Transmission failed. Next retry will skip CREATE, VALIDATE & SIGN and resume at TRANSMIT.");
                    return result;
                }
            }
            else
            {
                _logger.LogInformation("Skipping transmission - already completed (current status: {Status})", currentStatus);
                result.Pipeline.Transmit = PipelineStepResult.Success("Skipped - already transmitted", 200);
                result.CurrentStatus = InvoiceStatus.TRANSMITTED;
            }

            // ALL STEPS COMPLETED SUCCESSFULLY
            result.Success = true;
            result.Message = "Invoice created and submitted successfully through the entire pipeline";
            result.StatusCodes = 200;
            result.Pipeline.TotalExecutionTime = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Invoice submission completed successfully in {Time}ms. Final status: {Status}",
                result.Pipeline.TotalExecutionTime?.TotalMilliseconds ?? 0, result.CurrentStatus);

            // Track pipeline execution
            _telemetryService?.TrackPipelineExecution(
                result.InvoiceId ?? Guid.Empty,
                result.Success,
                result.Success ? null : "Failed",
                result.Pipeline.TotalExecutionTime ?? TimeSpan.Zero);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in invoice submission orchestration");

            result.Success = false;
            result.Message = "An unexpected error occurred during invoice submission";
            result.ErrorDetails = ex.Message;
            result.StatusCodes = 500;
            result.Pipeline.TotalExecutionTime = DateTime.UtcNow - startTime;

            // Track failed pipeline execution
            _telemetryService?.TrackPipelineExecution(
                result.InvoiceId ?? Guid.Empty,
                false,
                "Exception",
                result.Pipeline.TotalExecutionTime ?? TimeSpan.Zero);

            return result;
        }
    }

    /// <summary>
    /// Checks if an invoice already exists (for retry scenarios) - OPTIMIZED single query
    /// </summary>
    private async Task<(Guid InvoiceId, string IRN, InvoiceStatus Status)?> CheckExistingInvoiceAsync(
        CreateFIRSInvoiceCommand createData,
        CancellationToken cancellationToken)
    {
        try
        {
            // Only check if invoice number is provided (required for idempotency)
            if (string.IsNullOrWhiteSpace(createData.InvoiceNumber))
                return null;

            // Single optimized query - no navigation properties needed
            var invoice = await _context.Invoices
                .Where(i => i.Irn.Value.StartsWith(createData.InvoiceNumber) &&
                            i.BusinessId == createData.BusinessId)
                .Select(i => new { i.Id, i.Irn, i.InvoiceStatus })
                .AsNoTracking() // Performance: read-only query
                .FirstOrDefaultAsync(cancellationToken);

            return invoice != null
                ? (invoice.Id, invoice.Irn.Value, invoice.InvoiceStatus)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for existing invoice. Assuming new invoice.");
            return null; // If check fails, assume new invoice
        }
    }

    /// <summary>
    /// Determines if validation step should execute based on current status
    /// </summary>
    private static bool ShouldExecuteValidation(InvoiceStatus currentStatus) => currentStatus switch
    {
        InvoiceStatus.CREATED => true,
        InvoiceStatus.APPROVED => true,
        InvoiceStatus.VALIDATIONFAILED => true,     // RETRY from here
        InvoiceStatus.VALIDATED => false,           // Already done - skip
        InvoiceStatus.SIGNED => false,              // Already past this step
        InvoiceStatus.SIGNINGFAILED => false,       // Already past validation
        InvoiceStatus.TRANSMITTED => false,         // Already past this step
        InvoiceStatus.TRANSMISSIONFAILED => false,  // Already past validation
        _ => true                                   // Default: execute
    };

    /// <summary>
    /// Determines if signing step should execute based on current status
    /// </summary>
    private static bool ShouldExecuteSigning(InvoiceStatus currentStatus) => currentStatus switch
    {
        InvoiceStatus.VALIDATED => true,            // Normal flow after validation
        InvoiceStatus.APPROVED => true,        // RETRY from here
        InvoiceStatus.SIGNINGFAILED => true,        // RETRY from here
        InvoiceStatus.SIGNED => false,              // Already done - skip
        InvoiceStatus.TRANSMITTED => false,         // Already past this step
        InvoiceStatus.TRANSMISSIONFAILED => false,  // Already past signing
        _ => false                                  // Don't execute if not ready
    };

    /// <summary>
    /// Determines if transmission step should execute based on current status
    /// </summary>
    private static bool ShouldExecuteTransmission(InvoiceStatus currentStatus) => currentStatus switch
    {
        InvoiceStatus.SIGNED => true,               // Normal flow after signing
        InvoiceStatus.TRANSMISSIONFAILED => true,   // RETRY from here
        InvoiceStatus.TRANSMITTED => false,         // Already done - skip
        _ => false                                  // Don't execute if not ready
    };

    private static PipelineStepResult MapCreateResult(CreateFIRSInvoiceResult createResult) =>
        createResult.Success
            ? PipelineStepResult.Success(createResult.Message ?? "Invoice created", 201)
            : PipelineStepResult.Failed(createResult.Message ?? "Creation failed", 400, createResult.Message);

    private static PipelineStepResult MapValidateResult(ValidateInvoiceResult validateResult) =>
        validateResult.IsSuccess
            ? PipelineStepResult.Success(validateResult.Message ?? "Invoice validated", validateResult.StatusCodes)
            : PipelineStepResult.Failed(validateResult.Message ?? "Validation failed", validateResult.StatusCodes, validateResult.Message);

    private static PipelineStepResult MapSignResult(SignInvoiceResult signResult) =>
        signResult.IsSuccess
            ? PipelineStepResult.Success(signResult.Message ?? "Invoice signed", signResult.StatusCodes)
            : PipelineStepResult.Failed(signResult.Message ?? "Signing failed", signResult.StatusCodes, signResult.Message);

    private static PipelineStepResult MapTransmitResult(TransmitInvoiceResult transmitResult) =>
        transmitResult.IsSuccess
            ? PipelineStepResult.Success(transmitResult.Message ?? "Invoice transmitted", transmitResult.StatusCodes)
            : PipelineStepResult.Failed(transmitResult.Message ?? "Transmission failed", transmitResult.StatusCodes, transmitResult.Message);
}
