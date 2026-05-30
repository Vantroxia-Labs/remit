using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.RejectAllBroadcastInvoices;

public record RejectAllBroadcastInvoicesCommand(Guid BroadcastId) : IRequest<InvoiceBroadcastResult>, ITransactionalCommand;
