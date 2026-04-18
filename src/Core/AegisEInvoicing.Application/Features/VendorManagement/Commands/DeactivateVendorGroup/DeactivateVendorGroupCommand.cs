using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeactivateVendorGroup;

public record DeactivateVendorGroupCommand(Guid Id) : IRequest<VendorGroupResult>, ITransactionalCommand;
