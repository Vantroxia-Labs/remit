using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeactivateVendor;

public record DeactivateVendorCommand(Guid Id) : IRequest<VendorResult>, ITransactionalCommand;
