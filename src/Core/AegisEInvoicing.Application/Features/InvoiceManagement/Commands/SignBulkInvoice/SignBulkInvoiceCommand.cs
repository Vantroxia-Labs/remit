using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignBulkInvoice;

public sealed record SignBulkInvoiceCommand(
    List<Guid> InvoiceIds) : IRequest<SignBulkInvoiceResult>, ITransactionalCommand;
