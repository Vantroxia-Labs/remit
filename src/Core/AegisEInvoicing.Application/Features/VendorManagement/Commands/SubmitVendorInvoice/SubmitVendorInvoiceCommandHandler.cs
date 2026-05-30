using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.SaveVendorDraft;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.SubmitVendorInvoice;

public class SubmitVendorInvoiceCommandHandler(
    IApplicationDbContext context,
    IMediator mediator,
    IEmailService emailService,
    ILogger<SubmitVendorInvoiceCommandHandler> logger)
    : IRequestHandler<SubmitVendorInvoiceCommand, VendorPortalCommandResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IMediator _mediator = mediator;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<SubmitVendorInvoiceCommandHandler> _logger = logger;

    public async Task<VendorPortalCommandResult> Handle(SubmitVendorInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (!request.LineItems.Any())
            return new VendorPortalCommandResult(false, "At least one line item is required.");

        var bv = await _context.InvoiceBroadcastVendors
            .Include(x => x.InvoiceBroadcast)
                .ThenInclude(b => b.Business)
            .Include(x => x.Vendor)
            .FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);

        if (bv is null)
            return new VendorPortalCommandResult(false, "Invalid or expired link.");

        if (!bv.IsEmailVerified)
            return new VendorPortalCommandResult(false, "Email verification required before submitting.");

        var broadcast = bv.InvoiceBroadcast;
        if (broadcast.Status == BroadcastStatus.Deactivated || broadcast.IsExpired())
            return new VendorPortalCommandResult(false, "This broadcast is no longer active.");

        // Save/update draft first to persist line items
        var saveResult = await _mediator.Send(new SaveVendorDraftCommand(request.Token, request.LineItems), cancellationToken);
        if (!saveResult.IsSuccess)
            return saveResult;

        // Reload bv to get InvoiceId
        await _context.InvoiceBroadcastVendors.Entry(bv).ReloadAsync(cancellationToken);
        if (!bv.InvoiceId.HasValue)
            return new VendorPortalCommandResult(false, "Failed to create invoice draft.");

        var invoice = await _context.Invoices
            .FirstAsync(i => i.Id == bv.InvoiceId.Value, cancellationToken);

        if (broadcast.RequiresApproval && !broadcast.IsApprovalLocked)
        {
            invoice.UpdateStatus(InvoiceStatus.PENDING_APPROVAL);
            await _context.SaveChangesAsync(cancellationToken);

            // Notify tenant of pending approval
            try
            {
                var business = broadcast.Business;
                await _emailService.SendEmailAsync(new EmailMessage
                {
                    To = business.ContactEmail,
                    Subject = $"Invoice Submission Pending Approval – {broadcast.Title}",
                    HtmlBody = $"<p>Vendor <strong>{bv.Vendor.BusinessName}</strong> has submitted an invoice for broadcast <strong>{broadcast.Title}</strong> that requires your approval.</p>"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send approval notification for invoice {InvoiceId}", invoice.Id);
            }

            return new VendorPortalCommandResult(true, "Invoice submitted and pending tenant approval.");
        }
        else
        {
            // Transition to APPROVED so ValidateInvoiceCommandHandler can pick it up
            invoice.UpdateStatus(InvoiceStatus.APPROVED);
            await _context.SaveChangesAsync(cancellationToken);

            // Auto-approve: validate and sign
            var validateResult = await _mediator.Send(new ValidateInvoiceCommand(invoice.Id, broadcast.BusinessId), cancellationToken);
            if (!validateResult.IsSuccess)
            {
                _logger.LogWarning("Validation failed for vendor invoice {InvoiceId}: {Message}", invoice.Id, validateResult.Message);
                return new VendorPortalCommandResult(false, $"Invoice validation failed: {validateResult.Message}");
            }

            var signResult = await _mediator.Send(new SignInvoiceCommand(invoice.Id, broadcast.BusinessId), cancellationToken);
            if (!signResult.IsSuccess)
            {
                _logger.LogWarning("Signing failed for vendor invoice {InvoiceId}: {Message}", invoice.Id, signResult.Message);
                return new VendorPortalCommandResult(false, $"Invoice signing failed: {signResult.Message}");
            }

            broadcast.LockApprovalSetting();
            await _context.SaveChangesAsync(cancellationToken);

            return new VendorPortalCommandResult(true, "Invoice submitted and processed successfully.");
        }
    }
}
