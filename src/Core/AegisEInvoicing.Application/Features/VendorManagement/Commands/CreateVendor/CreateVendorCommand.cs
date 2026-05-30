using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateVendor;

public record CreateVendorCommand(
    string BusinessName,
    string Email,
    string? Phone,
    Guid VendorGroupId) : IRequest<VendorResult>, ITransactionalCommand;
