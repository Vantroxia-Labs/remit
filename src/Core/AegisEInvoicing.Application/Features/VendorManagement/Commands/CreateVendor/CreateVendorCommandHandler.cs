using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Entities.VendorManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateVendor;

public class CreateVendorCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<CreateVendorCommandHandler> logger) : IRequestHandler<CreateVendorCommand, VendorResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<CreateVendorCommandHandler> _logger = logger;

    public async Task<VendorResult> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorResult(false, "Unauthorized");

            var groupExists = await _context.VendorGroups
                .AnyAsync(g => g.Id == request.VendorGroupId && g.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (!groupExists)
                return new VendorResult(false, "Vendor group not found.");

            var duplicate = await _context.Vendors
                .AnyAsync(v => v.BusinessId == _currentUser.BusinessId.Value && v.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);

            if (duplicate)
                return new VendorResult(false, $"A vendor with email '{request.Email}' already exists.");

            var vendor = Vendor.Create(request.BusinessName, request.Email, request.VendorGroupId, _currentUser.BusinessId.Value, request.Phone);

            await _context.Vendors.AddAsync(vendor, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor {VendorId} created for business {BusinessId}", vendor.Id, _currentUser.BusinessId.Value);
            return new VendorResult(true, "Vendor created successfully.", vendor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            return new VendorResult(false, "An error occurred while creating the vendor.");
        }
    }
}
