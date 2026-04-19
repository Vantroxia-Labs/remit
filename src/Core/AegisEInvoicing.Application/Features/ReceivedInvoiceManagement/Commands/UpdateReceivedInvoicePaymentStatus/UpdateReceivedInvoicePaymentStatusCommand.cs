using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Commands.UpdateReceivedInvoicePaymentStatus;

public record UpdateReceivedInvoicePaymentStatusCommand(
    Guid ReceivedInvoiceId,
    string PaymentStatus,
    string? Reference) : IRequest<UpdateReceivedInvoicePaymentStatusResult>, ITransactionalCommand;
