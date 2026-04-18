using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateVendorGroup;

public class UpdateVendorGroupCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<UpdateVendorGroupCommandHandler> logger) : IRequestHandler<UpdateVendorGroupCommand, VendorGroupResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<UpdateVendorGroupCommandHandler> _logger = logger;

    public async Task<VendorGroupResult> Handle(UpdateVendorGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorGroupResult(false, "Unauthorized");

            var group = await _context.VendorGroups
                .FirstOrDefaultAsync(g => g.Id == request.Id && g.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (group is null)
                return new VendorGroupResult(false, "Vendor group not found.");

            var duplicate = await _context.VendorGroups
                .AnyAsync(g => g.Id != request.Id && g.BusinessId == _currentUser.BusinessId.Value && g.Name == request.Name.Trim(), cancellationToken);

            if (duplicate)
                return new VendorGroupResult(false, $"A vendor group named '{request.Name}' already exists.");

            group.Update(request.Name, request.Description);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor group {GroupId} updated", group.Id);
            return new VendorGroupResult(true, "Vendor group updated successfully.", group.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor group {GroupId}", request.Id);
            return new VendorGroupResult(false, "An error occurred while updating the vendor group.");
        }
    }
}
