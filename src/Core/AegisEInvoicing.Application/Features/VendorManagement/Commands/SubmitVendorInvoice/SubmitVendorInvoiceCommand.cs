using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.SubmitVendorInvoice;

public record SubmitVendorInvoiceCommand(
    string Token,
    List<VendorPortalLineItemDto> LineItems) : IRequest<VendorPortalCommandResult>, ITransactionalCommand;
