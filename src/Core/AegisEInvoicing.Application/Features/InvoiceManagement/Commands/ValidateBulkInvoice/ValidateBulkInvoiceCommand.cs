using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateBulkInvoice;

public sealed record ValidateBulkInvoiceCommand(
    List<Guid> InvoiceIds) : IRequest<ValidateBulkInvoiceResult>, ITransactionalCommand;


