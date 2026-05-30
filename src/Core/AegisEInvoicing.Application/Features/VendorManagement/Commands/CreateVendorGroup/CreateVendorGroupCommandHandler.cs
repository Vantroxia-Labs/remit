using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Entities.VendorManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateVendorGroup;

public class CreateVendorGroupCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<CreateVendorGroupCommandHandler> logger) : IRequestHandler<CreateVendorGroupCommand, VendorGroupResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<CreateVendorGroupCommandHandler> _logger = logger;

    public async Task<VendorGroupResult> Handle(CreateVendorGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new VendorGroupResult(false, "Unauthorized");

            var duplicate = await _context.VendorGroups
                .AnyAsync(g => g.BusinessId == _currentUser.BusinessId.Value && g.Name == request.Name.Trim(), cancellationToken);

            if (duplicate)
                return new VendorGroupResult(false, $"A vendor group named '{request.Name}' already exists.");

            var group = VendorGroup.Create(request.Name, request.Description, _currentUser.BusinessId.Value);

            await _context.VendorGroups.AddAsync(group, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor group {GroupId} created for business {BusinessId}", group.Id, _currentUser.BusinessId.Value);
            return new VendorGroupResult(true, "Vendor group created successfully.", group.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor group");
            return new VendorGroupResult(false, "An error occurred while creating the vendor group.");
        }
    }
}
