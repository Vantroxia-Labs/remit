using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.UpdateParty;

public class UpdatePartyCommandHandler : IRequestHandler<UpdatePartyCommand, PartyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdatePartyCommandHandler> _logger;

    public UpdatePartyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<UpdatePartyCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PartyResult> Handle(UpdatePartyCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            _logger.LogWarning("Unauthorized attempt to create party");
            throw new AuthenticationException("User authentication required");
        }

        if (!_currentUser.BusinessId.HasValue)
        {
            _logger.LogWarning("Business Not Found");
            throw new NotFoundException("Business Not Found");
        }

        // Get existing party
        var party = await _context.Parties
            .FirstOrDefaultAsync(p => p.Id == request.Id
            && p.BusinessID == _currentUser.BusinessId.Value,
            cancellationToken);

        if (party is null)
        {
            _logger.LogWarning("Attempt to update non-existent party {PartyId}", request.Id);
            throw new NotFoundException("Party Not Found");
        }

        // Create value objects
        var tin = TIN.Create(request.TaxIdentificationNumber);
        var address = Address.Create(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.Country,
            request.Address.PostalCode ?? string.Empty);

        // Update party properties
        party.UpdateName(request.Name);
        party.UpdateContactInfo(request.Phone, request.Email);
        party.UpdateTaxIdentificationNumber(tin);
        party.UpdateAddress(address);

        // Save changes
        _context.Parties.Update(party);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated party {PartyId}", request.Id);
        return new PartyResult(true, "Successfully updated party", request.Id);
    }
}