using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateVendor;

public class UpdateVendorCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<UpdateVendorCommandHandler> logger) : IRequestHandler<UpdateVendorCommand, VendorResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<UpdateVendorCommandHandler> _logger = logger;

    public async Task<VendorResult> Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorResult(false, "Unauthorized");

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (vendor is null)
                return new VendorResult(false, "Vendor not found.");

            var groupExists = await _context.VendorGroups
                .AnyAsync(g => g.Id == request.VendorGroupId && g.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (!groupExists)
                return new VendorResult(false, "Vendor group not found.");

            var duplicate = await _context.Vendors
                .AnyAsync(v => v.Id != request.Id && v.BusinessId == _currentUser.BusinessId.Value && v.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);

            if (duplicate)
                return new VendorResult(false, $"Another vendor with email '{request.Email}' already exists.");

            vendor.Update(request.BusinessName, request.Email, request.VendorGroupId, request.Phone);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor {VendorId} updated", vendor.Id);
            return new VendorResult(true, "Vendor updated successfully.", vendor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {VendorId}", request.Id);
            return new VendorResult(false, "An error occurred while updating the vendor.");
        }
    }
}
