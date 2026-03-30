using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitBulkInvoice;

public record TransmitBulkInvoiceCommand(
    List<Guid> InvoiceIds) : IRequest<TransmitBulkInvoiceResult>, ITransactionalCommand;
