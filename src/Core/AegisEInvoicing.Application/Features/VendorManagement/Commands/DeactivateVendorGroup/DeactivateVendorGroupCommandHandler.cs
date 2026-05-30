using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeactivateVendorGroup;

public class DeactivateVendorGroupCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<DeactivateVendorGroupCommandHandler> logger) : IRequestHandler<DeactivateVendorGroupCommand, VendorGroupResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<DeactivateVendorGroupCommandHandler> _logger = logger;

    public async Task<VendorGroupResult> Handle(DeactivateVendorGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorGroupResult(false, "Unauthorized");

            var group = await _context.VendorGroups
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (group is null)
                return new VendorGroupResult(false, "Vendor group not found.");

            if (group.IsDeleted)
                return new VendorGroupResult(false, "Vendor group is already deactivated.");

            group.MarkAsDeleted(_currentUser.UserId);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor group {GroupId} deactivated", group.Id);
            return new VendorGroupResult(true, "Vendor group deactivated successfully.", group.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating vendor group {GroupId}", request.Id);
            return new VendorGroupResult(false, "An error occurred while deactivating the vendor group.");
        }
    }
}
