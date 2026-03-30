using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoiceByIrn;

public sealed record ValidateInvoiceByIrnCommand(
    string Irn = null!) : IRequest<ValidateInvoiceResult>, ITransactionalCommand;
