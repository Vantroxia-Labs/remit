using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;

public sealed record SignInvoiceCommand(
    Guid InvoiceId,
    Guid? BusinessId = null) : IRequest<SignInvoiceResult>, ITransactionalCommand;
