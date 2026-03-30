using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateParty;

public record CreatePartyCommand(
    string Name,
    string Phone,
    string Email,
    string TaxIdentificationNumber,
    CreateAddressDto Address,
    string Description) : IRequest<PartyResult>, ITransactionalCommand;