using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateInvoiceBroadcast;

public record UpdateInvoiceBroadcastCommand(
    Guid Id,
    string Title,
    string? Note) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
