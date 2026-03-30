using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.RejectInvoice;

/// <summary>
/// Command to reject a pending invoice (ClientAdmin only)
/// </summary>
public record RejectInvoiceCommand(
    Guid InvoiceId,
    string RejectionReason) : IRequest<RejectInvoiceResult>;
