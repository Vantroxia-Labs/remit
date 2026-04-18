using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.ToggleVendorStatus;

public record ToggleVendorStatusCommand(Guid Id) : IRequest<VendorResult>, ITransactionalCommand;
