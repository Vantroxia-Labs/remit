using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.RejectInvoice;

public class RejectInvoiceCommandHandler : IRequestHandler<RejectInvoiceCommand, RejectInvoiceResult>
{
    private readonly IInvoiceApprovalService _approvalService;

    public RejectInvoiceCommandHandler(IInvoiceApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    public async Task<RejectInvoiceResult> Handle(RejectInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Validate user authorization
        var authValidation = _approvalService.ValidateUserAuthorization();
        if (!authValidation.IsValid)
        {
            return MapValidationToResult(authValidation);
        }

        // Fetch invoice
        var invoice = await _approvalService.GetInvoiceForApprovalAsync(request.InvoiceId, cancellationToken);

        // Validate invoice
        var invoiceValidation = _approvalService.ValidateInvoiceForApproval(invoice, request.InvoiceId);
        if (!invoiceValidation.IsValid)
        {
            return MapValidationToResult(invoiceValidation);
        }

        // Process rejection
        var result = await _approvalService.RejectInvoiceAsync(
            invoice!,
            request.RejectionReason,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return RejectInvoiceResult.BadRequest(result.ErrorMessage!);
        }

        return RejectInvoiceResult.Success(
            result.InvoiceId!.Value,
            $"Invoice {result.Irn} rejected successfully");
    }

    private static RejectInvoiceResult MapValidationToResult(InvoiceApprovalValidationResult validation)
    {
        return validation.StatusCode switch
        {
            HttpStatusCodes.Forbidden => RejectInvoiceResult.AuthorizationError(validation.ErrorMessage!),
            HttpStatusCodes.NotFound => RejectInvoiceResult.NotFound(validation.ErrorMessage!),
            _ => RejectInvoiceResult.BadRequest(validation.ErrorMessage!)
        };
    }
}
