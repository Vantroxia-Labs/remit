using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateVendorGroup;

public record UpdateVendorGroupCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest<VendorGroupResult>, ITransactionalCommand;
