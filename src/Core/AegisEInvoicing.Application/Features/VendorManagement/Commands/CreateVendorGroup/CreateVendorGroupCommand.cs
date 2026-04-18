using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateVendorGroup;

public record CreateVendorGroupCommand(
    string Name,
    string? Description) : IRequest<VendorGroupResult>, ITransactionalCommand;
