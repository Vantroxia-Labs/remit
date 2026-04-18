using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.DeactivateBusinessItem;

public record DeactivateBusinessItemCommand(Guid Id) : IRequest<BusinessItemResult>, ITransactionalCommand;
