using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using AegisEInvoicing.Interswitch.Interfaces;
using AegisEInvoicing.Interswitch.Models.Requests.UpdateStatus;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.RejectAllBroadcastInvoices;

public class RejectAllBroadcastInvoicesCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IInterswitchHttpClient interswitchHttpClient,
    IEmailService emailService,
    ILogger<RejectAllBroadcastInvoicesCommandHandler> logger) : IRequestHandler<RejectAllBroadcastInvoicesCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IInterswitchHttpClient _interswitchHttpClient = interswitchHttpClient;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<RejectAllBroadcastInvoicesCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(RejectAllBroadcastInvoicesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new InvoiceBroadcastResult(false, "Unauthorized");

            var broadcastExists = await _context.InvoiceBroadcasts
                .AnyAsync(b => b.Id == request.BroadcastId && b.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (!broadcastExists)
                return new InvoiceBroadcastResult(false, "Broadcast not found.");

            var broadcastVendors = await _context.InvoiceBroadcastVendors
                .Include(bv => bv.Invoice)
                .Include(bv => bv.Vendor)
                .Include(bv => bv.InvoiceBroadcast)
                    .ThenInclude(b => b.Business)
                .Where(bv =>
                    bv.InvoiceBroadcastId == request.BroadcastId &&
                    bv.InvoiceId != null &&
                    bv.Invoice != null &&
                    bv.Invoice.PaymentStatus == PaymentStatus.Pending)
                .ToListAsync(cancellationToken);

            string? template = null;
            try
            {
                template = await File.ReadAllTextAsync(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Email", "VendorInvoiceRejected.html"),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load VendorInvoiceRejected email template");
            }

            var succeeded = 0;
            foreach (var bv in broadcastVendors)
            {
                if (bv.Invoice is null) continue;

                var invoice = bv.Invoice;

                // Only call NRS for invoices that went through the no-approval path
                var wentToNrs = invoice.InvoiceStatus == InvoiceStatus.SIGNED
                    || invoice.InvoiceStatus == InvoiceStatus.TRANSMITTED
                    || invoice.InvoiceStatus == InvoiceStatus.COMPLETELYTRANSMITTED;

                if (wentToNrs && invoice.Irn is not null)
                {
                    try
                    {
                        await _interswitchHttpClient.UpdateStatusAsync(new UpdateStatusRequest
                        {
                            PaymentStatus = "REJECTED",
                            IRN = invoice.Irn.Value,
                            Reference = invoice.PaymentReference
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update NRS rejection status for invoice {InvoiceId}", invoice.Id);
                    }
                }

                invoice.UpdatePaymentStatus(PaymentStatus.Rejected);
                succeeded++;

                // Notify vendor
                if (template is not null)
                {
                    try
                    {
                        var body = template
                            .Replace("{vendorName}", bv.Vendor.BusinessName)
                            .Replace("{broadcastTitle}", bv.InvoiceBroadcast.Title)
                            .Replace("{tenantName}", bv.InvoiceBroadcast.Business.Name)
                            .Replace("{invoiceCode}", invoice.InvoiceCode ?? invoice.Id.ToString());

                        await _emailService.SendEmailAsync(new EmailMessage
                        {
                            To = bv.Vendor.Email,
                            Subject = $"Invoice Rejected – {bv.InvoiceBroadcast.Title}",
                            HtmlBody = body
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send rejection email to vendor {VendorId}", bv.VendorId);
                    }
                }
            }

            if (succeeded > 0)
                await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Rejected {Count} pending invoices for broadcast {BroadcastId}", succeeded, request.BroadcastId);
            return new InvoiceBroadcastResult(true, $"Rejected {succeeded} pending invoice(s).", request.BroadcastId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting all invoices for broadcast {BroadcastId}", request.BroadcastId);
            return new InvoiceBroadcastResult(false, "An error occurred while rejecting the invoices.");
        }
    }
}
