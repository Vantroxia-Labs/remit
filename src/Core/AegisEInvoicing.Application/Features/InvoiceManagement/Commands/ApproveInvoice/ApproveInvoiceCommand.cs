using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ApproveInvoice;

/// <summary>
/// Command to approve a pending invoice (ClientAdmin only)
/// </summary>
public record ApproveInvoiceCommand(
    Guid InvoiceId,
    string? ApprovalComments = null) : IRequest<ApproveInvoiceResult>;
