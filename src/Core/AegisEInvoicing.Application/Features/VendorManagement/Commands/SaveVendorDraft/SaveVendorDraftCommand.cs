using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.SaveVendorDraft;

public record SaveVendorDraftCommand(
    string Token,
    List<VendorPortalLineItemDto> LineItems) : IRequest<VendorPortalCommandResult>, ITransactionalCommand;
