using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.DeactivateParty;

public class DeactivatePartyCommandHandler : IRequestHandler<DeactivatePartyCommand, PartyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeactivatePartyCommandHandler> _logger;

    public DeactivatePartyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<DeactivatePartyCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PartyResult> Handle(DeactivatePartyCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            _logger.LogWarning("Unauthorized attempt to deactivate party");
            throw new AuthenticationException("User authentication required");
        }

        if (!_currentUser.BusinessId.HasValue)
        {
            _logger.LogWarning("Business Not Found");
            throw new NotFoundException("Business Not Found");
        }

        var party = await _context.Parties
            .FirstOrDefaultAsync(p => p.Id == request.Id
                && p.BusinessID == _currentUser.BusinessId.Value,
                cancellationToken);

        if (party is null)
        {
            _logger.LogWarning("Attempt to deactivate non-existent party {PartyId}", request.Id);
            throw new NotFoundException("Party Not Found");
        }

        if (party.IsDeleted)
        {
            return new PartyResult(false, "Party is already deactivated", request.Id);
        }

        party.MarkAsDeleted(_currentUser.UserId.Value);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deactivated party {PartyId}", request.Id);
        return new PartyResult(true, "Successfully deactivated party", request.Id);
    }
}
