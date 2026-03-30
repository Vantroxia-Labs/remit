using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ProcessInvoiceTransmission;

/// <summary>
/// Handler for processing invoice transmission requests from queue
/// </summary>
public class ProcessInvoiceTransmissionCommandHandler : IRequestHandler<ProcessInvoiceTransmissionCommand, ProcessInvoiceTransmissionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ProcessInvoiceTransmissionCommandHandler> _logger;

    public ProcessInvoiceTransmissionCommandHandler(
        IApplicationDbContext context,
        ILogger<ProcessInvoiceTransmissionCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProcessInvoiceTransmissionResult> Handle(
        ProcessInvoiceTransmissionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing invoice transmission for IRN {IRN} with status {Status}",
                request.Irn, request.Status);

            // Parse and validate IRN
            var irnValue = IRN.CreateFromString(request.Irn);

            // Find the invoice
            var invoice = await _context.Invoices
                .Include(i => i.Business)
                .Where(i => i.Irn.Value == irnValue.Value && !i.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice with IRN {IRN} not found", request.Irn);
                return new ProcessInvoiceTransmissionResult
                {
                    IsSuccess = false,
                    Message = $"Invoice with IRN {request.Irn} not found"
                };
            }

            // Process based on status
            var result = await ProcessInvoiceStatusUpdate(invoice, request.Status, request.Metadata, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully processed invoice transmission for IRN {IRN}", request.Irn);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice transmission for IRN {IRN}", request.Irn);

            return new ProcessInvoiceTransmissionResult
            {
                IsSuccess = false,
                Message = "Failed to process invoice transmission",
                ErrorDetails = ex.Message
            };
        }
    }

    private async Task<ProcessInvoiceTransmissionResult> ProcessInvoiceStatusUpdate(
        Domain.Entities.InvoiceManagement.Invoice invoice,
        InvoiceStatus status,
        Dictionary<string, object>? metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (status)
            {
                case InvoiceStatus.TRANSMITTING:
                    invoice.UpdateStatus(Domain.Enums.InvoiceStatus.SUBMITTED);
                    _logger.LogInformation("Invoice {IRN} status updated to TRANSMITTING", invoice.Irn.Value);
                    break;

                case InvoiceStatus.TRANSMITTED:
                    invoice.UpdateStatus(Domain.Enums.InvoiceStatus.TRANSMITTED);
                    if (metadata?.ContainsKey("submissionId") == true)
                    {
                        invoice.SetFIRSSubmissionId(metadata["submissionId"].ToString() ?? string.Empty);
                    }
                    invoice.SetSubmittedToFIRSAt(DateTimeOffset.UtcNow);
                    _logger.LogInformation("Invoice {IRN} status updated to TRANSMITTED", invoice.Irn.Value);
                    break;

                case InvoiceStatus.ACKNOWLEDGED:
                    invoice.UpdateStatus(Domain.Enums.InvoiceStatus.ACKNOWLEDGED);
                    _logger.LogInformation("Invoice {IRN} acknowledged by FIRS", invoice.Irn.Value);
                    break;

                case InvoiceStatus.FAILED:
                    invoice.UpdateStatus(Domain.Enums.InvoiceStatus.FAILED);
                    _logger.LogWarning("Invoice {IRN} transmission failed", invoice.Irn.Value);
                    break;

                default:
                    _logger.LogWarning("Unknown invoice status {Status} for IRN {IRN}", status, invoice.Irn.Value);
                    return new ProcessInvoiceTransmissionResult
                    {
                        IsSuccess = false,
                        Message = $"Unknown invoice status: {status}"
                    };
            }

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync(cancellationToken);

            return new ProcessInvoiceTransmissionResult
            {
                IsSuccess = true,
                Message = $"Invoice status updated to {status}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice status for IRN {IRN}", invoice.Irn.Value);
            throw;
        }
    }
}