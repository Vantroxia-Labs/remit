using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceDraft;

public record DeleteInvoiceDraftCommand(Guid DraftId) : IRequest<DeleteInvoiceDraftResult>;
