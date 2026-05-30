using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartyById;

public class GetPartyByIdQueryHandler : IRequestHandler<GetPartyByIdQuery, GetPartyByIdResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetPartyByIdQueryHandler> _logger;

    public GetPartyByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetPartyByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<GetPartyByIdResult> Handle(GetPartyByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
            {
                _logger.LogWarning("Business Not Found");
                throw new NotFoundException("Business Not Found");
            }

            var party = await _context.Parties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.BusinessID == _currentUser.BusinessId.Value, cancellationToken);

            if (party is null)
            {
                _logger.LogWarning("Party {PartyId} not found", request.Id);
                throw new NotFoundException("Party Not Found");
            }

            var partyDto = new PartyDto(
                party.Id,
                party.Name,
                party.Phone,
                party.Email,
                party.TaxIdentificationNumber.Value,
                new AddressDto(
                    party.Address.Street,
                    party.Address.City,
                    party.Address.State,
                    party.Address.Country,
                    party.Address.PostalCode,
                    party.Address.Lga),
                party.CreatedAt,
                party.UpdatedAt,
                party.CreatedBy,
                party.UpdatedBy ?? Guid.Empty);

            _logger.LogDebug("Successfully retrieved party {PartyId}", request.Id);
            return new GetPartyByIdResult
            {
                Success = true,
                Message = "Successfully retrieved party",
                Party = partyDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving party {PartyId}: {Message}", request.Id, ex.Message);
            throw new UnprocessableEntityException("Party cannot be returned at this time. Please try again later.");
        }
    }
}
