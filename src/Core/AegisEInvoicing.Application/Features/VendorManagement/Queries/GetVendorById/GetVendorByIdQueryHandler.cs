using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorById;

public class GetVendorByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetVendorByIdQueryHandler> logger) : IRequestHandler<GetVendorByIdQuery, VendorDto?>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetVendorByIdQueryHandler> _logger = logger;

    public async Task<VendorDto?> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return null;

            return await _context.Vendors
                .AsNoTracking()
                .Include(v => v.VendorGroup)
                .Where(v => v.Id == request.Id && v.BusinessId == _currentUser.BusinessId.Value)
                .Select(v => new VendorDto(
                    v.Id,
                    v.BusinessName,
                    v.Email,
                    v.Phone,
                    v.Status,
                    v.BusinessId,
                    v.VendorGroupId,
                    v.VendorGroup.Name,
                    v.CreatedAt,
                    v.UpdatedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor {VendorId}", request.Id);
            return null;
        }
    }
}
