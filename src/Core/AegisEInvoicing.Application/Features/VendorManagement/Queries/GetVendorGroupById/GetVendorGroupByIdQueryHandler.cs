using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorGroupById;

public class GetVendorGroupByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetVendorGroupByIdQueryHandler> logger) : IRequestHandler<GetVendorGroupByIdQuery, VendorGroupDto?>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetVendorGroupByIdQueryHandler> _logger = logger;

    public async Task<VendorGroupDto?> Handle(GetVendorGroupByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return null;

            return await _context.VendorGroups
                .AsNoTracking()
                .Where(g => g.Id == request.Id && g.BusinessId == _currentUser.BusinessId.Value)
                .Select(g => new VendorGroupDto(
                    g.Id,
                    g.Name,
                    g.Description,
                    g.BusinessId,
                    g.Vendors.Count,
                    g.CreatedAt,
                    g.UpdatedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor group {GroupId}", request.Id);
            return null;
        }
    }
}
