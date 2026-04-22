using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateParty;

public class CreatePartyCommandHandler : IRequestHandler<CreatePartyCommand, PartyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreatePartyCommandHandler> _logger;

    public CreatePartyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<CreatePartyCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PartyResult> Handle(CreatePartyCommand request, CancellationToken cancellationToken)
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

        // Verify business exists
        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == _currentUser.BusinessId.Value, cancellationToken);

        if (!businessExists)
        {
            _logger.LogWarning("Attempt to create party for non-existent business {BusinessId}", _currentUser.BusinessId.Value);
            throw new NotFoundException("Business Not Found");
        }

        // Create value objects
        var tin = TIN.Create(request.TaxIdentificationNumber);
        var address = Address.Create(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.Country,
            request.Address.PostalCode ?? string.Empty,
            request.Address.Lga);

        // Create Party entity
        var party = Party.Create(
            request.Name,
            request.Phone,
            request.Email,
            tin,
            address,
            _currentUser.BusinessId.Value,
            request.Description);

        party.MarkAsCreated(_currentUser.UserId.Value);

        var isExists = await _context.Parties.AnyAsync(
        p => p.BusinessID == _currentUser.BusinessId.Value &&
        (p.TaxIdentificationNumber.Value == request.TaxIdentificationNumber ||
            p.Email == request.Email),
        cancellationToken);

        if (isExists)
            throw new ConflictException("Party with phone number/email address already exists for this business.");

        // Save to database
        await _context.Parties.AddAsync(party);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created party {PartyId} for business {BusinessId}", party.Id, _currentUser.BusinessId.Value);
        return new PartyResult(true,
                               "Party Successfully Created",
                               party.Id);
    }
}
