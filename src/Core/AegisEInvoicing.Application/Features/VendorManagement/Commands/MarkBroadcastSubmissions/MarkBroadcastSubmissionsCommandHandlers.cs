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

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.MarkBroadcastSubmissions;

public class MarkBroadcastSubmissionsPaidCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IInterswitchHttpClient interswitchHttpClient,
    ILogger<MarkBroadcastSubmissionsPaidCommandHandler> logger) : IRequestHandler<MarkBroadcastSubmissionsPaidCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IInterswitchHttpClient _interswitchHttpClient = interswitchHttpClient;
    private readonly ILogger<MarkBroadcastSubmissionsPaidCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(MarkBroadcastSubmissionsPaidCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new InvoiceBroadcastResult(false, "Unauthorized");

            if (request.InvoiceIds.Count == 0)
                return new InvoiceBroadcastResult(false, "No invoice IDs provided.");

            var invoices = await _context.Invoices
                .Where(i => request.InvoiceIds.Contains(i.Id) && i.BusinessId == _currentUser.BusinessId.Value)
                .ToListAsync(cancellationToken);

            var succeeded = 0;
            foreach (var invoice in invoices)
            {
                // Only call NRS for invoices that went through the no-approval path (already on NRS)
                var wentToNrs = invoice.InvoiceStatus == InvoiceStatus.SIGNED
                    || invoice.InvoiceStatus == InvoiceStatus.TRANSMITTED
                    || invoice.InvoiceStatus == InvoiceStatus.ACKNOWLEDGED;

                if (wentToNrs && invoice.Irn is not null)
                {
                    try
                    {
                        await _interswitchHttpClient.UpdateStatusAsync(new UpdateStatusRequest
                        {
                            PaymentStatus = "PAID",
                            IRN = invoice.Irn.Value,
                            Reference = invoice.PaymentReference
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update NRS payment status for invoice {InvoiceId}", invoice.Id);
                    }
                }

                invoice.UpdatePaymentStatus(PaymentStatus.Paid);
                succeeded++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked {Count} invoices as Paid for business {BusinessId}", succeeded, _currentUser.BusinessId.Value);
            return new InvoiceBroadcastResult(true, $"Marked {succeeded} invoice(s) as paid.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking submissions as paid");
            return new InvoiceBroadcastResult(false, "An error occurred.");
        }
    }
}

public class MarkBroadcastSubmissionsRejectedCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IInterswitchHttpClient interswitchHttpClient,
    IEmailService emailService,
    ILogger<MarkBroadcastSubmissionsRejectedCommandHandler> logger) : IRequestHandler<MarkBroadcastSubmissionsRejectedCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IInterswitchHttpClient _interswitchHttpClient = interswitchHttpClient;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<MarkBroadcastSubmissionsRejectedCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(MarkBroadcastSubmissionsRejectedCommand request, CancellationToken cancellationToken)
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

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked {Count} invoices as Rejected for business {BusinessId}", succeeded, _currentUser.BusinessId.Value);
            return new InvoiceBroadcastResult(true, $"Marked {succeeded} invoice(s) as rejected.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking submissions as rejected");
            return new InvoiceBroadcastResult(false, "An error occurred.");
        }
    }
}
