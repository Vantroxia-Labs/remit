using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoicePaymentStatus;

public record UpdateInvoicePaymentStatusCommand(
    Guid InvoiceId,
    PaymentStatus PaymentStatus,
    string? Reference,
    decimal? Amount) : IRequest<UpdateInvoicePaymentStatusResult>, ITransactionalCommand;