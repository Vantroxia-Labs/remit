using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Queries.GetBusinessItemById;

public record GetBusinessItemByIdQuery(Guid Id) : IRequest<BusinessItemByIdResult>;