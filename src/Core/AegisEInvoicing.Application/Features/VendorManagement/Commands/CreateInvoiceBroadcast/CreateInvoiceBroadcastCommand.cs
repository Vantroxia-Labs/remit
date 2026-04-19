using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateInvoiceBroadcast;

public record CreateInvoiceBroadcastCommand(
    string Title,
    string InvoiceTypeCode,
    DateOnly DueDate,
    bool RequiresApproval,
    string Currency,
    string? Note,
    // Target specific vendors. If null AND VendorGroupId is set, targets all vendors in that group.
    List<Guid>? VendorIds = null,
    // Target all vendors in this group (ignored if VendorIds is specified).
    Guid? VendorGroupId = null) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
