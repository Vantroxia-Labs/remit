using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.DeleteBusinessItem;

public class DeleteBusinessItemCommandHandler : IRequestHandler<DeleteBusinessItemCommand, BusinessItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeleteBusinessItemCommandHandler> _logger;

    public DeleteBusinessItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<DeleteBusinessItemCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BusinessItemResult> Handle(DeleteBusinessItemCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            _logger.LogWarning("Unauthorized attempt to delete business item {BusinessItemId}", request.Id);
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
            _logger.LogWarning("Attempt to delete non-existent business item {BusinessItemId}", request.Id);
            throw new NotFoundException("Business item not found");
        }

        // Check if item has associated invoice items (business logic)
        var hasInvoiceItems = await _context.InvoiceItems
            .AnyAsync(ii => ii.BusinessItemId == request.Id
            && ii.Invoice.BusinessId == _currentUser.BusinessId.Value,
            cancellationToken);

        if (hasInvoiceItems)
        {
            _logger.LogWarning("Attempt to delete business item {BusinessItemId} with associated invoice items", request.Id);
            throw new ConflictException("Cannot delete business item that has associated invoice items.");
        }

        businessItem.MarkAsDeleted(_currentUser.UserId.Value);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully deleted business item {BusinessItemId}", request.Id);
        return new BusinessItemResult(true, "Successfully deleted business item");
    }
}
