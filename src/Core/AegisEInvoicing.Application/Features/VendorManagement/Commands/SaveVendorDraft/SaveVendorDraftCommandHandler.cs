using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Application.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.SaveVendorDraft;

public class SaveVendorDraftCommandHandler(
    IApplicationDbContext context,
    ILogger<SaveVendorDraftCommandHandler> logger)
    : IRequestHandler<SaveVendorDraftCommand, VendorPortalCommandResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILogger<SaveVendorDraftCommandHandler> _logger = logger;

    public async Task<VendorPortalCommandResult> Handle(SaveVendorDraftCommand request, CancellationToken cancellationToken)
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
            return new VendorPortalCommandResult(false, "Email verification required before saving a draft.");

        var broadcast = bv.InvoiceBroadcast;
        if (broadcast.Status == Domain.Enums.BroadcastStatus.Deactivated || broadcast.IsExpired())
            return new VendorPortalCommandResult(false, "This broadcast is no longer active.");

        var business = broadcast.Business;

        // Find or create Party for the vendor
        var partyTin = bv.Vendor.Email; // TIN format validation is disabled; use email as placeholder
        var existingParty = await _context.Parties
            .FirstOrDefaultAsync(p => p.BusinessID == business.Id && p.TaxIdentificationNumber.Value == partyTin, cancellationToken);

        Guid partyId;
        if (existingParty is not null)
        {
            partyId = existingParty.Id;
        }
        else
        {
            var tin = TIN.Create(partyTin);
            var address = Address.Create("-", "-", "-", "Nigeria", "");
            var party = Party.Create(
                bv.Vendor.BusinessName,
                bv.Vendor.Phone ?? "-",
                bv.Vendor.Email,
                tin,
                address,
                business.Id,
                $"Auto-created from vendor portal broadcast: {broadcast.Title}");
            party.MarkAsCreated(business.Id); // system-created, use businessId as actor
            await _context.Parties.AddAsync(party, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            partyId = party.Id;
        }

        // Parse InvoiceType from broadcast
        if (!int.TryParse(broadcast.InvoiceTypeCode, out var invoiceTypeCode))
            invoiceTypeCode = 396;

        var invoiceType = InvoiceType.Create(broadcast.InvoiceTypeCode, invoiceTypeCode);
        var currency = Currency.Create(broadcast.Currency, broadcast.Currency);

        Invoice invoice;

        if (bv.InvoiceId.HasValue)
        {
            // Update existing draft — reload and clear items
            invoice = await _context.Invoices
                .Include(i => i.InvoiceLine)
                .FirstAsync(i => i.Id == bv.InvoiceId.Value, cancellationToken);

            foreach (var item in invoice.InvoiceLine.ToList())
                _context.InvoiceItems.Remove(item);
        }
        else
        {
            // Create new draft invoice
            invoice = Invoice.CreateDraft(business.Id, business.InvoicePrefix, invoiceType, currency);
            invoice.SetParty(partyId);
            invoice.MarkAsCreated(business.Id);
            await _context.Invoices.AddAsync(invoice, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            bv.AssignInvoice(invoice.Id);
        }

        // Add line items
        foreach (var li in request.LineItems)
        {
            var item = InvoiceItem.CreateFreeText(invoice.Id, li.Description, li.Quantity, li.UnitPrice, li.UnitOfMeasure);
            item.MarkAsCreated(business.Id);
            await _context.InvoiceItems.AddAsync(item, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved vendor draft for broadcast vendor {BvId}, invoice {InvoiceId}", bv.Id, invoice.Id);
        return new VendorPortalCommandResult(true, "Draft saved successfully.");
    }
}
