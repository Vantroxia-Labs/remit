using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.DeactivateParty;

public record DeactivatePartyCommand(Guid Id) : IRequest<PartyResult>, ITransactionalCommand;
