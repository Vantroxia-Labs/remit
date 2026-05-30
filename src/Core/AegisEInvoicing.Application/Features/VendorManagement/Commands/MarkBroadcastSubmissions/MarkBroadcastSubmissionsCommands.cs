using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.MarkBroadcastSubmissions;

public record MarkBroadcastSubmissionsPaidCommand(List<Guid> InvoiceIds) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
public record MarkBroadcastSubmissionsRejectedCommand(List<Guid> InvoiceIds) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
