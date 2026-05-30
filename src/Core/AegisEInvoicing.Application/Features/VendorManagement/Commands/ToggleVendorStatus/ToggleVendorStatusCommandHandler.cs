using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.ToggleVendorStatus;

public class ToggleVendorStatusCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<ToggleVendorStatusCommandHandler> logger) : IRequestHandler<ToggleVendorStatusCommand, VendorResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ToggleVendorStatusCommandHandler> _logger = logger;

    public async Task<VendorResult> Handle(ToggleVendorStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorResult(false, "Unauthorized");

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (vendor is null)
                return new VendorResult(false, "Vendor not found.");

            if (vendor.Status == VendorStatus.Active)
            {
                vendor.Deactivate();
                await _context.SaveChangesAsync(cancellationToken);
                return new VendorResult(true, "Vendor deactivated.", vendor.Id);
            }
            else
            {
                vendor.Activate();
                await _context.SaveChangesAsync(cancellationToken);
                return new VendorResult(true, "Vendor activated.", vendor.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling vendor status {VendorId}", request.Id);
            return new VendorResult(false, "An error occurred while updating the vendor status.");
        }
    }
}
