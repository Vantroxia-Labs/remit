using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DismissBroadcastSubmissions;

public record DismissBroadcastSubmissionsCommand(List<Guid> InvoiceIds) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
