using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorPortalForm;

public class GetVendorPortalFormQueryHandler(
    IApplicationDbContext context,
    ILogger<GetVendorPortalFormQueryHandler> logger)
    : IRequestHandler<GetVendorPortalFormQuery, VendorPortalFormDto?>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILogger<GetVendorPortalFormQueryHandler> _logger = logger;

    public async Task<VendorPortalFormDto?> Handle(GetVendorPortalFormQuery request, CancellationToken cancellationToken)
    {
        var bv = await _context.InvoiceBroadcastVendors
            .AsNoTracking()
            .Include(x => x.InvoiceBroadcast)
                .ThenInclude(b => b.Business)
            .Include(x => x.Vendor)
            .FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);

        if (bv is null)
        {
            _logger.LogWarning("Vendor portal form requested for unknown token");
            return null;
        }

        var broadcast = bv.InvoiceBroadcast;
        bool isClosed = !broadcast.IsActive || broadcast.DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

        // Mask email: show first 2 chars + *** + @domain
        var email = bv.Vendor.Email;
        var atIndex = email.IndexOf('@');
        var maskedEmail = atIndex > 2
            ? $"{email[..2]}***{email[atIndex..]}"
            : $"***{(atIndex >= 0 ? email[atIndex..] : "")}";

        return new VendorPortalFormDto(
            broadcast.Title,
            broadcast.DueDate,
            broadcast.InvoiceTypeCode,
            broadcast.Currency,
            broadcast.RequiresApproval,
            broadcast.Note,
            broadcast.Business.Name,
            bv.Vendor.BusinessName,
            maskedEmail,
            isClosed);
    }
}
