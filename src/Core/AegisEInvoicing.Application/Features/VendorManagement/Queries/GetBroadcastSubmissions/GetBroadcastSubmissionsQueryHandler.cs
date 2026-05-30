using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastSubmissions;

public class GetBroadcastSubmissionsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetBroadcastSubmissionsQueryHandler> logger) : IRequestHandler<GetBroadcastSubmissionsQuery, PaginatedList<BroadcastVendorSubmissionDto>>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetBroadcastSubmissionsQueryHandler> _logger = logger;

    public async Task<PaginatedList<BroadcastVendorSubmissionDto>> Handle(GetBroadcastSubmissionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new PaginatedList<BroadcastVendorSubmissionDto>([], 0, request.PageNumber, request.PageSize);

            // Verify the broadcast belongs to this business
            var broadcastExists = await _context.InvoiceBroadcasts
                .AnyAsync(b => b.Id == request.BroadcastId && b.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (!broadcastExists)
                return new PaginatedList<BroadcastVendorSubmissionDto>([], 0, request.PageNumber, request.PageSize);

            var query = _context.InvoiceBroadcastVendors
                .AsNoTracking()
                .Include(bv => bv.Vendor)
                .Include(bv => bv.Invoice)
                .Where(bv => bv.InvoiceBroadcastId == request.BroadcastId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(bv => bv.EmailVerifiedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(bv => new BroadcastVendorSubmissionDto(
                    bv.Id,
                    bv.VendorId,
                    bv.Vendor.BusinessName,
                    bv.Vendor.Email,
                    bv.IsEmailVerified,
                    bv.InvoiceId,
                    bv.Invoice != null ? bv.Invoice.InvoiceCode : null,
                    bv.Invoice != null ? bv.Invoice.Irn.Value : null,
                    bv.Invoice != null ? bv.Invoice.InvoiceStatus.ToString() : null,
                    bv.Invoice != null ? bv.Invoice.PaymentStatus.ToString() : null,
                    bv.EmailVerifiedAt,
                    bv.Invoice != null ? bv.Invoice.SubmittedToFIRSAt : null))
                .ToListAsync(cancellationToken);

            return new PaginatedList<BroadcastVendorSubmissionDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving broadcast submissions for {BroadcastId}", request.BroadcastId);
            return new PaginatedList<BroadcastVendorSubmissionDto>([], 0, request.PageNumber, request.PageSize);
        }
    }
}
