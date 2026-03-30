using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.DeleteParty;

public class DeletePartyCommandHandler : IRequestHandler<DeletePartyCommand, PartyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeletePartyCommandHandler> _logger;

    public DeletePartyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<DeletePartyCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PartyResult> Handle(DeletePartyCommand request, CancellationToken cancellationToken)
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
            _logger.LogWarning("Attempt to delete non-existent party {PartyId}", request.Id);
            throw new NotFoundException("Party Not Found");
        }

        // Check if party has invoices (business logic)
        var hasInvoices = await _context.Invoices
            .AnyAsync(i => i.PartyId == request.Id, cancellationToken);

        if (hasInvoices)
        {
            _logger.LogWarning("Attempt to delete party {PartyId} with associated invoices", request.Id);
            throw new UnprocessableEntityException("Cannot delete party that has associated invoices.");
        }

        party.MarkAsDeleted(_currentUser.UserId.Value);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted party {PartyId}", request.Id);
        return new PartyResult(true, "Successfully deleted party", request.Id);
    }
}
