using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice;

public sealed record TransmitInvoiceCommand(
    Guid InvoiceId,
    Guid? BusinessId = null) : IRequest<TransmitInvoiceResult>, ITransactionalCommand;
