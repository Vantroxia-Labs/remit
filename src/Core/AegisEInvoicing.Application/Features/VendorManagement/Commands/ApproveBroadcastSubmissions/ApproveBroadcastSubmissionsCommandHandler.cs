using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.ApproveBroadcastSubmissions;

public class ApproveBroadcastSubmissionsCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IMediator mediator,
    ILogger<ApproveBroadcastSubmissionsCommandHandler> logger) : IRequestHandler<ApproveBroadcastSubmissionsCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<ApproveBroadcastSubmissionsCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(ApproveBroadcastSubmissionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new InvoiceBroadcastResult(false, "Unauthorized");

            if (request.InvoiceIds.Count == 0)
                return new InvoiceBroadcastResult(false, "No invoice IDs provided.");

            var invoices = await _context.Invoices
                .Where(i => request.InvoiceIds.Contains(i.Id)
                    && i.BusinessId == _currentUser.BusinessId.Value
                    && i.InvoiceStatus == InvoiceStatus.PENDING_APPROVAL)
                .ToListAsync(cancellationToken);

            if (invoices.Count == 0)
                return new InvoiceBroadcastResult(false, "No pending-approval invoices found.");

            var succeeded = 0;
            var failures = new List<string>();

            foreach (var invoice in invoices)
            {
                var validateResult = await _mediator.Send(new ValidateInvoiceCommand(invoice.Id, _currentUser.BusinessId.Value), cancellationToken);
                if (!validateResult.IsSuccess)
                {
                    _logger.LogWarning("Validation failed for invoice {InvoiceId}: {Message}", invoice.Id, validateResult.Message);
                    failures.Add($"{invoice.InvoiceCode ?? invoice.Id.ToString()}: validation failed");
                    continue;
                }

                var signResult = await _mediator.Send(new SignInvoiceCommand(invoice.Id, _currentUser.BusinessId.Value), cancellationToken);
                if (!signResult.IsSuccess)
                {
                    _logger.LogWarning("Signing failed for invoice {InvoiceId}: {Message}", invoice.Id, signResult.Message);
                    failures.Add($"{invoice.InvoiceCode ?? invoice.Id.ToString()}: signing failed");
                    continue;
                }

                succeeded++;
            }

            // Lock approval setting on the broadcast once at least one invoice goes to NRS
            if (succeeded > 0)
            {
                var broadcastIds = await _context.InvoiceBroadcastVendors
                    .Where(bv => request.InvoiceIds.Contains(bv.InvoiceId!.Value))
                    .Select(bv => bv.InvoiceBroadcastId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var broadcasts = await _context.InvoiceBroadcasts
                    .Where(b => broadcastIds.Contains(b.Id))
                    .ToListAsync(cancellationToken);

                foreach (var broadcast in broadcasts)
                    broadcast.LockApprovalSetting();

                await _context.SaveChangesAsync(cancellationToken);
            }

            var message = failures.Count > 0
                ? $"Approved {succeeded} invoice(s). {failures.Count} failed: {string.Join("; ", failures)}"
                : $"Approved {succeeded} invoice(s) and submitted to NRS.";

            _logger.LogInformation("Approved {Count} broadcast invoices for business {BusinessId}", succeeded, _currentUser.BusinessId.Value);
            return new InvoiceBroadcastResult(succeeded > 0, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving broadcast submissions");
            return new InvoiceBroadcastResult(false, "An error occurred while approving the submissions.");
        }
    }
}
