using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.UpdateParty;

public record UpdatePartyCommand(
    Guid Id,
    string Name,
    string Phone,
    string Email,
    string TaxIdentificationNumber,
    UpdateAddressDto Address) : IRequest<PartyResult>, ITransactionalCommand;