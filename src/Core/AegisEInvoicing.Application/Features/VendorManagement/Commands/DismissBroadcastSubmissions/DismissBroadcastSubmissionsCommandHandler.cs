using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DismissBroadcastSubmissions;

public class DismissBroadcastSubmissionsCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEmailService emailService,
    ILogger<DismissBroadcastSubmissionsCommandHandler> logger) : IRequestHandler<DismissBroadcastSubmissionsCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<DismissBroadcastSubmissionsCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(DismissBroadcastSubmissionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new InvoiceBroadcastResult(false, "Unauthorized");

            if (request.InvoiceIds.Count == 0)
                return new InvoiceBroadcastResult(false, "No invoice IDs provided.");

            var broadcastVendors = await _context.InvoiceBroadcastVendors
                .Include(bv => bv.Invoice)
                .Include(bv => bv.Vendor)
                .Include(bv => bv.InvoiceBroadcast)
                    .ThenInclude(b => b.Business)
                .Where(bv => bv.InvoiceId != null && request.InvoiceIds.Contains(bv.InvoiceId!.Value))
                .ToListAsync(cancellationToken);

            string? template = null;
            try
            {
                template = await File.ReadAllTextAsync(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Email", "VendorInvoiceDismissed.html"),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load VendorInvoiceDismissed email template");
            }

            var succeeded = 0;
            foreach (var bv in broadcastVendors)
            {
                if (bv.Invoice is null) continue;

                bv.Invoice.UpdatePaymentStatus(PaymentStatus.Dismissed);
                succeeded++;

                if (template is not null)
                {
                    try
                    {
                        var body = template
                            .Replace("{vendorName}", bv.Vendor.BusinessName)
                            .Replace("{broadcastTitle}", bv.InvoiceBroadcast.Title)
                            .Replace("{tenantName}", bv.InvoiceBroadcast.Business.Name)
                            .Replace("{invoiceCode}", bv.Invoice.InvoiceCode ?? bv.Invoice.Id.ToString());

                        await _emailService.SendEmailAsync(new EmailMessage(
                            bv.Vendor.Email,
                            $"Invoice Not Selected – {bv.InvoiceBroadcast.Title}",
                            body), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send dismissal email to vendor {VendorId}", bv.VendorId);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Dismissed {Count} broadcast submissions for business {BusinessId}", succeeded, _currentUser.BusinessId.Value);
            return new InvoiceBroadcastResult(true, $"Dismissed {succeeded} submission(s).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing broadcast submissions");
            return new InvoiceBroadcastResult(false, "An error occurred while dismissing the submissions.");
        }
    }
}
