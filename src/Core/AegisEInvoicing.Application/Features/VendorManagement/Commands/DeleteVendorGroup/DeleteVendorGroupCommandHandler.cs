using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeleteVendorGroup;

public class DeleteVendorGroupCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<DeleteVendorGroupCommandHandler> logger) : IRequestHandler<DeleteVendorGroupCommand, VendorGroupResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<DeleteVendorGroupCommandHandler> _logger = logger;

    public async Task<VendorGroupResult> Handle(DeleteVendorGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorGroupResult(false, "Unauthorized");

            var group = await _context.VendorGroups
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (group is null)
                return new VendorGroupResult(false, "Vendor group not found.");

            var hasVendors = await _context.Vendors
                .AnyAsync(v => v.VendorGroupId == request.Id, cancellationToken);

            if (hasVendors)
                return new VendorGroupResult(false, "Cannot delete a vendor group that has vendors. Move or delete the vendors first.");

            _context.VendorGroups.Remove(group);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor group {GroupId} deleted", group.Id);
            return new VendorGroupResult(true, "Vendor group deleted successfully.", group.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vendor group {GroupId}", request.Id);
            return new VendorGroupResult(false, "An error occurred while deleting the vendor group.");
        }
    }
}
