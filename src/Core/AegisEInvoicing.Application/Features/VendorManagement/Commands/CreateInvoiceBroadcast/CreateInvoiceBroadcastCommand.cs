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
    /// <summary>Target specific vendors. If null AND VendorGroupId is set, targets all vendors in that group.</summary>
    List<Guid>? VendorIds = null,
    /// <summary>Target all vendors in this group (ignored if VendorIds is specified).</summary>
    Guid? VendorGroupId = null,
    string? FrontendBaseUrl = null) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
