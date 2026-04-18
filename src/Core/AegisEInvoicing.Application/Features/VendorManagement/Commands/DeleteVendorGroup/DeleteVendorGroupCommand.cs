using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeleteVendorGroup;

public record DeleteVendorGroupCommand(Guid Id) : IRequest<VendorGroupResult>, ITransactionalCommand;
