using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateVendor;

public record UpdateVendorCommand(
    Guid Id,
    string BusinessName,
    string Email,
    string? Phone,
    Guid VendorGroupId) : IRequest<VendorResult>, ITransactionalCommand;
