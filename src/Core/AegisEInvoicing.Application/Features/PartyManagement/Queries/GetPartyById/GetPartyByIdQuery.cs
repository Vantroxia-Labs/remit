using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartyById;

public record GetPartyByIdQuery(Guid Id) : IRequest<GetPartyByIdResult>;