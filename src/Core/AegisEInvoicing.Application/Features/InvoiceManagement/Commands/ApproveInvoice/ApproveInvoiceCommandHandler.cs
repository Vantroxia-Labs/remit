using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ApproveInvoice;

public class ApproveInvoiceCommandHandler : IRequestHandler<ApproveInvoiceCommand, ApproveInvoiceResult>
{
    private readonly IInvoiceApprovalService _approvalService;

    public ApproveInvoiceCommandHandler(IInvoiceApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    public async Task<ApproveInvoiceResult> Handle(ApproveInvoiceCommand request, CancellationToken cancellationToken)
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

        // Process approval
        var result = await _approvalService.ApproveInvoiceAsync(
            invoice!,
            request.ApprovalComments,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ApproveInvoiceResult.BadRequest(result.ErrorMessage!);
        }

        return ApproveInvoiceResult.Success(
            result.InvoiceId!.Value,
            $"Invoice {result.Irn} approved successfully");
    }

    private static ApproveInvoiceResult MapValidationToResult(InvoiceApprovalValidationResult validation)
    {
        return validation.StatusCode switch
        {
            HttpStatusCodes.Forbidden => ApproveInvoiceResult.AuthorizationError(validation.ErrorMessage!),
            HttpStatusCodes.NotFound => ApproveInvoiceResult.NotFound(validation.ErrorMessage!),
            _ => ApproveInvoiceResult.BadRequest(validation.ErrorMessage!)
        };
    }
}
