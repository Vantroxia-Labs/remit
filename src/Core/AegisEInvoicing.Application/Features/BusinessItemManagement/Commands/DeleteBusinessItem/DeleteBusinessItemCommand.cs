using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.DeleteBusinessItem;

public record DeleteBusinessItemCommand(Guid Id) : IRequest<BusinessItemResult>, ITransactionalCommand;