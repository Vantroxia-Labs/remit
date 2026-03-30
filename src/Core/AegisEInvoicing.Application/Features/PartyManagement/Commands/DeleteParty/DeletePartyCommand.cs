using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.DeleteParty;

public record DeletePartyCommand(Guid Id) : IRequest<PartyResult>, ITransactionalCommand;