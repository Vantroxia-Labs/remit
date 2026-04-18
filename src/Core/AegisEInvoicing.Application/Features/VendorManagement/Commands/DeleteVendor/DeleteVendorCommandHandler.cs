using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeleteVendor;

public class DeleteVendorCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<DeleteVendorCommandHandler> logger) : IRequestHandler<DeleteVendorCommand, VendorResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<DeleteVendorCommandHandler> _logger = logger;

    public async Task<VendorResult> Handle(DeleteVendorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorResult(false, "Unauthorized");

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (vendor is null)
                return new VendorResult(false, "Vendor not found.");

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor {VendorId} deleted", vendor.Id);
            return new VendorResult(true, "Vendor deleted successfully.", vendor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vendor {VendorId}", request.Id);
            return new VendorResult(false, "An error occurred while deleting the vendor.");
        }
    }
}
