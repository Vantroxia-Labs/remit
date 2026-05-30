using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.ExtendBroadcastDueDate;

public record ExtendBroadcastDueDateCommand(
    Guid Id,
    DateOnly NewDueDate) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
