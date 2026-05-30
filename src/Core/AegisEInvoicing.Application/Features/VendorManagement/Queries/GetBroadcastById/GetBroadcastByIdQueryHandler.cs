using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastById;

public class GetBroadcastByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetBroadcastByIdQueryHandler> logger) : IRequestHandler<GetBroadcastByIdQuery, InvoiceBroadcastDto?>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetBroadcastByIdQueryHandler> _logger = logger;

    public async Task<InvoiceBroadcastDto?> Handle(GetBroadcastByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return null;

            var broadcast = await _context.InvoiceBroadcasts
                .AsNoTracking()
                .Include(b => b.BroadcastVendors)
                    .ThenInclude(bv => bv.Vendor)
                .Include(b => b.BroadcastVendors)
                    .ThenInclude(bv => bv.Invoice)
                .Where(b => b.Id == request.Id && b.BusinessId == _currentUser.BusinessId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (broadcast is null)
                return null;

            var vendorDtos = broadcast.BroadcastVendors
                .Select(bv => new BroadcastVendorDto(
                    bv.Id,
                    bv.VendorId,
                    bv.Vendor.BusinessName,
                    bv.Vendor.Email,
                    bv.IsEmailVerified,
                    bv.InvoiceId,
                    bv.Invoice?.InvoiceStatus.ToString(),
                    bv.Invoice?.PaymentStatus.ToString(),
                    bv.EmailVerifiedAt))
                .ToList();

            return new InvoiceBroadcastDto(
                broadcast.Id,
                broadcast.Title,
                broadcast.InvoiceTypeCode,
                broadcast.DueDate,
                broadcast.RequiresApproval,
                broadcast.IsApprovalLocked,
                broadcast.Status,
                broadcast.Currency,
                broadcast.Note,
                broadcast.BusinessId,
                vendorDtos,
                broadcast.CreatedAt,
                broadcast.UpdatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving broadcast {BroadcastId}", request.Id);
            return null;
        }
    }
}
