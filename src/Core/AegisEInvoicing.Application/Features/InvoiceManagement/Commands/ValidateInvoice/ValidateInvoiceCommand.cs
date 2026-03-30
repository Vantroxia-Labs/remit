using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;

public sealed record ValidateInvoiceCommand(
    Guid InvoiceId,
    Guid? BusinessId = null) : IRequest<ValidateInvoiceResult>, ITransactionalCommand;
