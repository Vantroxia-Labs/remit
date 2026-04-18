using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeactivateVendor;

public class DeactivateVendorCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<DeactivateVendorCommandHandler> logger) : IRequestHandler<DeactivateVendorCommand, VendorResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<DeactivateVendorCommandHandler> _logger = logger;

    public async Task<VendorResult> Handle(DeactivateVendorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorResult(false, "Unauthorized");

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (vendor is null)
                return new VendorResult(false, "Vendor not found.");

            if (vendor.IsDeleted)
                return new VendorResult(false, "Vendor is already deactivated.");

            vendor.MarkAsDeleted(_currentUser.UserId);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor {VendorId} deactivated", vendor.Id);
            return new VendorResult(true, "Vendor deactivated successfully.", vendor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating vendor {VendorId}", request.Id);
            return new VendorResult(false, "An error occurred while deactivating the vendor.");
        }
    }
}
