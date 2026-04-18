using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.DeactivateBusinessItem;

public class DeactivateBusinessItemCommandHandler : IRequestHandler<DeactivateBusinessItemCommand, BusinessItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeactivateBusinessItemCommandHandler> _logger;

    public DeactivateBusinessItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<DeactivateBusinessItemCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BusinessItemResult> Handle(DeactivateBusinessItemCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            _logger.LogWarning("Unauthorized attempt to deactivate business item {BusinessItemId}", request.Id);
            throw new AuthenticationException("User authentication required");
        }

        if (!_currentUser.BusinessId.HasValue)
        {
            _logger.LogWarning("Business not found");
            throw new ForbiddenException("Business not found");
        }

        var businessItem = await _context.BusinessItems
            .FirstOrDefaultAsync(bi => bi.Id == request.Id
                && bi.BusinessID == _currentUser.BusinessId.Value,
                cancellationToken);

        if (businessItem is null)
        {
            _logger.LogWarning("Attempt to deactivate non-existent business item {BusinessItemId}", request.Id);
            throw new NotFoundException("Business item not found");
        }

        if (businessItem.IsDeleted)
        {
            return new BusinessItemResult(false, "Business item is already deactivated");
        }

        businessItem.MarkAsDeleted(_currentUser.UserId.Value);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deactivated business item {BusinessItemId}", request.Id);
        return new BusinessItemResult(true, "Successfully deactivated business item", request.Id);
    }
}
