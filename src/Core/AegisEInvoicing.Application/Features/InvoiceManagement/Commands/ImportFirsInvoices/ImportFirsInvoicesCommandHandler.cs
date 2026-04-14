using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ImportFirsInvoices;

public sealed class ImportFirsInvoicesCommandHandler(
    IApplicationDbContext context,
    IFirsMbsApiClient mbsClient,
    IReferenceDataCacheService referenceCache,
    ILogger<ImportFirsInvoicesCommandHandler> logger) : IRequestHandler<ImportFirsInvoicesCommand, ImportFirsInvoicesResult>
{
    private const int PageSize = 1000;

    public async Task<ImportFirsInvoicesResult> Handle(
        ImportFirsInvoicesCommand request,
        CancellationToken cancellationToken)
    {
        var imported = new List<string>();
        var skipped = new List<string>();
        var failed = new List<string>();
        var errors = new List<string>();

        // Authenticate
        string token;
        try
        {
            token = await mbsClient.LoginAsync(request.Email, request.Password, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authenticate with FIRS MBS portal");
            return new ImportFirsInvoicesResult { Success = false, Message = $"Authentication failed: {ex.Message}" };
        }

        // Warm the reference-data cache
        // Populates invoice-type names, currency names, payment-means names, etc.
        // from the live FIRS API so all lookups below resolve to real names.
        try
        {
            await referenceCache.RefreshCacheAsync(cancellationToken);
            logger.LogInformation("Reference data cache refreshed successfully before import");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reference data cache refresh failed — names will fall back to raw codes");
        }

        // Paginate through all invoices
        var allItems = new List<MbsInvoiceListItem>();
        var page = 1;

        while (true)
        {
            MbsInvoiceListData? pageData;
            try
            {
                pageData = await mbsClient.GetInvoicePageAsync(token, page, PageSize, cancellationToken);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to fetch page {page}: {ex.Message}";
                logger.LogError(ex, msg);
                errors.Add(msg);
                break;
            }

            if (pageData is null || pageData.Items.Count == 0)
                break;

            allItems.AddRange(pageData.Items);
            logger.LogInformation("Fetched page {Page}: {Count} invoices (running total: {Total})",
                page, pageData.Items.Count, allItems.Count);

            if (!pageData.Page.HasNextPage)
                break;

            page++;
        }

        logger.LogInformation("Total invoices fetched from FIRS MBS: {Total}", allItems.Count);

        // Process each invoice
        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IRN.IsValidIRNFormat(item.Irn))
            {
                logger.LogWarning("Skipping invalid IRN format: {IRN}", item.Irn);
                skipped.Add(item.Irn);
                continue;
            }

            var alreadyExists = await context.Invoices
                .AsNoTracking()
                .AnyAsync(i => i.Irn.Value == item.Irn.ToUpperInvariant(), cancellationToken);

            if (alreadyExists)
            {
                logger.LogInformation("IRN {IRN} already exists — skipping", item.Irn);
                skipped.Add(item.Irn);
                continue;
            }

            try
            {
                var detail = await mbsClient.GetInvoiceDetailAsync(token, item.Irn, cancellationToken);
                if (detail is null)
                {
                    logger.LogWarning("No detail response for IRN {IRN} — skipping", item.Irn);
                    failed.Add(item.Irn);
                    errors.Add($"No detail response for IRN {item.Irn}");
                    continue;
                }

                await ImportSingleInvoiceAsync(detail, item.EntryStatus, cancellationToken);
                imported.Add(item.Irn);
                logger.LogInformation("Imported IRN {IRN}", item.Irn);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import IRN {IRN}", item.Irn);
                failed.Add(item.Irn);
                errors.Add($"IRN {item.Irn}: {ex.Message}");
            }
        }

        return new ImportFirsInvoicesResult
        {
            Success = true,
            Message = $"Import complete. Imported: {imported.Count}, Skipped: {skipped.Count}, Failed: {failed.Count}",
            TotalFetched = allItems.Count,
            TotalImported = imported.Count,
            TotalSkipped = skipped.Count,
            TotalFailed = failed.Count,
            ImportedIRNs = imported,
            SkippedIRNs = skipped,
            FailedIRNs = failed,
            Errors = errors
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Single-invoice import
    // ────────────────────────────────────────────────────────────────────────────

    private async Task ImportSingleInvoiceAsync(
        MbsInvoiceDetail detail,
        string entryStatus,
        CancellationToken cancellationToken)
    {
        // 1. Resolve business by supplier TIN
        var supplierTin = detail.SupplierParty?.Tin ?? string.Empty;
        var business = await context.Businesses
            .FirstOrDefaultAsync(b => b.TaxIdentificationNumber.Value == supplierTin && !b.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No business found with TIN '{supplierTin}' (supplier: {detail.SupplierParty?.PartyName}). " +
                "Ensure the supplier TIN matches a registered business.");

        // 2. Resolve user for audit fields
        var createdByUser = await context.Users
            .Where(u => u.BusinessId == business.Id && !u.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
        var createdById = createdByUser?.Id ?? Guid.Empty;

        // 3. Find or create Party (customer)
        var partyId = await ResolvePartyAsync(detail.CustomerParty, business.Id, createdById, cancellationToken);

        // 4. Build value objects
        var irn = IRN.CreateFromString(detail.Irn);
        var issueDate = ParseDateOnly(detail.IssueDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var issueTime = ParseTimeOnly(detail.IssueTime);
        var dueDate = ParseDateOnly(detail.DueDate);
        var invoiceType = MapInvoiceType(detail.InvoiceTypeCode);
        var currency = MapCurrency(detail.DocumentCurrencyCode);
        var deliveryPeriod = MapDeliveryPeriod(detail.DeliveryPeriod, issueDate);
        var paymentMeans = MapPaymentMeans(detail.PaymentMeans);

        // 5. Create invoice
        var invoice = Invoice.CreateFromImport(
            businessId: business.Id,
            partyId: partyId,
            irn: irn,
            invoicePrefix: business.InvoicePrefix,
            issueDate: issueDate,
            issueTime: issueTime,
            invoiceType: invoiceType,
            currency: currency,
            deliveryPeriod: deliveryPeriod,
            paymentMeans: paymentMeans,
            invoiceSource: InvoiceSource.FIRS,
            note: detail.Note,
            paymentTerms: detail.PaymentTermsNote,
            dueDate: dueDate,
            environmentMode: business.AppEnvironmentMode);

        invoice.CreatedBy = createdById;
        // 6. QR code (skip if business lacks cert/key)
        if (!string.IsNullOrEmpty(business.Certificate) && !string.IsNullOrEmpty(business.PublicKey))
        {
            var qrCode = InvoiceQrService.GenerateQrCode(invoice.Irn, business.Certificate!, business.PublicKey!);
            invoice.SetQRCode(qrCode, []);
        }
        else
        {
            logger.LogWarning("Business {BusinessId} has no certificate/public key — QR code skipped for IRN {IRN}",
                business.Id, detail.Irn);
        }

        // 7. Invoice line items
        foreach (var line in detail.InvoiceLine)
        {
            var businessItemId = await ResolveBusinessItemAsync(line, business.Id, createdById, cancellationToken);
            var unitPrice = line.Price?.PriceAmount ?? line.LineExtensionAmount;

            DiscountFee? discountFee = line.DiscountAmount > 0
                ? DiscountFee.Create(line.DiscountAmount, FeeStandardUnit.NGN)
                : line.DiscountRate > 0
                    ? DiscountFee.Create(line.DiscountRate, FeeStandardUnit.Percent)
                    : null;

            AdditionalFee? additionalFee = line.FeeAmount > 0
                ? AdditionalFee.Create(line.FeeAmount, FeeStandardUnit.NGN)
                : line.FeeRate > 0
                    ? AdditionalFee.Create(line.FeeRate, FeeStandardUnit.Percent)
                    : null;

            invoice.AddInvoiceItem(InvoiceItem.Create(
                businessItemId, invoice.Id, line.InvoicedQuantity, unitPrice, discountFee, additionalFee));
        }

        await context.Invoices.AddAsync(invoice, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // 8. Approval history + final status
        var finalStatus = MapEntryStatus(entryStatus);
        await CreateApprovalHistoryAsync(invoice.Id, finalStatus, createdById, cancellationToken);

        invoice.UpdateStatus(finalStatus);
        context.Invoices.Update(invoice);
        await context.SaveChangesAsync(cancellationToken);
    }

    // ── Party ────────────────────────────────────────────────────────────────

    private async Task<Guid> ResolvePartyAsync(
        MbsParty? customer,
        Guid businessId,
        Guid createdById,
        CancellationToken cancellationToken)
    {
        if (customer is null)
            throw new InvalidOperationException("Invoice detail contains no customer party.");

        var existing = await context.Parties
            .FirstOrDefaultAsync(p => p.TaxIdentificationNumber.Value == customer.Tin &&
                                      p.BusinessID == businessId && !p.IsDeleted, cancellationToken);

        if (existing is null && !string.IsNullOrWhiteSpace(customer.Email))
        {
            existing = await context.Parties
                .FirstOrDefaultAsync(p => p.Email == customer.Email &&
                                          p.BusinessID == businessId && !p.IsDeleted, cancellationToken);
        }

        if (existing is not null)
        {
            logger.LogInformation("Reusing existing party {PartyId} (TIN={TIN})", existing.Id, customer.Tin);
            return existing.Id;
        }

        var party = Party.Create(
            customer.PartyName,
            customer.Telephone,
            customer.Email,
            TIN.Create(customer.Tin),
            MapAddress(customer.PostalAddress),
            businessId,
            customer.BusinessDescription ?? string.Empty);

        party.CreatedBy = createdById;
        await context.Parties.AddAsync(party, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created new party {PartyId} (TIN={TIN})", party.Id, customer.Tin);
        return party.Id;
    }

    // ── BusinessItem ──────────────────────────────────────────────────────────

    private async Task<Guid> ResolveBusinessItemAsync(
        MbsInvoiceLine line,
        Guid businessId,
        Guid createdById,
        CancellationToken cancellationToken)
    {
        var itemName = line.Item?.Name ?? "Unnamed Item";
        var itemDesc = line.Item?.Description ?? string.Empty;
        var categoryName = string.IsNullOrWhiteSpace(line.ProductCategory) ? "General" : line.ProductCategory;
        var svcCodeValue = string.IsNullOrWhiteSpace(line.HsnCode) ? "DEFAULT" : line.HsnCode;
        var svcCodeName = string.IsNullOrWhiteSpace(line.ServiceCategory) ? itemName : line.ServiceCategory;
        var unitPrice = line.Price?.PriceAmount ?? line.LineExtensionAmount;

        // Find or create ItemCategory
        var itemCategory = await context.ItemCategories
            .FirstOrDefaultAsync(ic => ic.Name == categoryName &&
                                       ic.BusinessID == businessId && !ic.IsDeleted, cancellationToken);

        if (itemCategory is null)
        {
            itemCategory = ItemCategory.Create(categoryName, $"Auto-created for import: {categoryName}", businessId);
            await context.ItemCategories.AddAsync(itemCategory, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created ItemCategory '{Category}' for business {BusinessId}", categoryName, businessId);
        }

        // Find or create BusinessItem by name
        var existing = await context.BusinessItems
            .Include(bi => bi.ItemCategories).ThenInclude(ic => ic.ItemCategory)
            .FirstOrDefaultAsync(bi => bi.Name == itemName &&
                                       bi.BusinessID == businessId && !bi.IsDeleted, cancellationToken);

        if (existing is not null)
        {
            if (!existing.BelongsToCategory(itemCategory.Id))
                existing.AddCategory(itemCategory.Id);

            if (existing.UnitPrice != unitPrice)
                existing.UpdatePriceFromErp(unitPrice);

            if (existing.ItemDescription != itemDesc)
                existing.UpdateDescription(itemDesc);

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Reusing BusinessItem {ItemId} ('{Name}')", existing.Id, itemName);
            return existing.Id;
        }

        var businessItem = BusinessItem.Create(
            businessId, itemName,
            ItemType.Service,
            ServiceCode.Create(svcCodeValue, svcCodeName),
            itemCategory.Id, itemDesc, unitPrice);

        businessItem.CreatedBy = createdById;
        await context.BusinessItems.AddAsync(businessItem, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created BusinessItem {ItemId} ('{Name}')", businessItem.Id, itemName);
        return businessItem.Id;
    }

    // ── Approval history ──────────────────────────────────────────────────────

    private async Task CreateApprovalHistoryAsync(
        Guid invoiceId,
        InvoiceStatus finalStatus,
        Guid createdById,
        CancellationToken cancellationToken)
    {
        var stages = new List<(InvoiceStatus Status, string Message)>
        {
            (InvoiceStatus.CREATED,   ResponseMessages.INVOICE_CREATED_SUCCESS),
            (InvoiceStatus.APPROVED,  ResponseMessages.INVOICE_APPROVED_SUCCESS),
            (InvoiceStatus.VALIDATED, "Invoice validated by FIRS"),
            (InvoiceStatus.SIGNED,    "Invoice signed by FIRS")
        };

        if (finalStatus == InvoiceStatus.TRANSMITTING)
            stages.Add((InvoiceStatus.TRANSMITTING, "Invoice transmitting to FIRS"));
        else if (finalStatus == InvoiceStatus.TRANSMITTED)
            stages.Add((InvoiceStatus.TRANSMITTED, "Invoice transmitted to FIRS"));

        foreach (var (status, message) in stages)
        {
            var history = InvoiceApprovalHistory.Create(invoiceId, status, message);
            history.CreatedBy = createdById;
            await context.InvoiceApprovalHistories.AddAsync(history, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static InvoiceStatus MapEntryStatus(string entryStatus) =>
        entryStatus.ToUpperInvariant() switch
        {
            "TRANSMITTING" => InvoiceStatus.TRANSMITTING,
            "TRANSMITTED" => InvoiceStatus.TRANSMITTED,
            _ => InvoiceStatus.SIGNED
        };

    private InvoiceType MapInvoiceType(string code)
    {
        var name = referenceCache.GetInvoiceTypeName(code) ?? $"Invoice Type {code}";
        return int.TryParse(code, out var numericCode)
            ? InvoiceType.Create(name, numericCode)
            : InvoiceType.Create(name, 380);
    }

    private Currency MapCurrency(string code)
    {
        var upper = code.ToUpperInvariant();
        var name = referenceCache.GetCurrencyName(upper) ?? upper;
        return Currency.Create(name, upper);
    }

    private static DeliveryPeriod MapDeliveryPeriod(MbsDeliveryPeriod? period, DateOnly issueDate)
    {
        var start = period is not null ? ParseDateOnly(period.StartDate) ?? issueDate : issueDate;
        var end = period is not null ? ParseDateOnly(period.EndDate) ?? issueDate.AddDays(30) : issueDate.AddDays(30);
        if (end < start) end = start.AddDays(30);
        return DeliveryPeriod.Create(start, end);
    }

    private PaymentMeans MapPaymentMeans(IReadOnlyList<MbsPaymentMeans> means)
    {
        var first = means.FirstOrDefault();
        if (first is null) return PaymentMeans.Create("30", referenceCache.GetPaymentMeansName("30") ?? "Credit Transfer");

        var code = first.PaymentMeansCode;
        var name = referenceCache.GetPaymentMeansName(code) ?? $"Payment Code {code}";
        return PaymentMeans.Create(code, name);
    }

    private static Address MapAddress(MbsPostalAddress? addr) =>
        addr is not null
            ? Address.Create(addr.StreetName, addr.CityName, addr.State ?? "Lagos", addr.Country, addr.PostalZone ?? string.Empty)
            : Address.Create("N/A", "N/A", string.Empty, "NG", string.Empty);

    private static (string Name, decimal Percent) ExtractTaxCategory(IReadOnlyList<MbsTaxTotal> taxTotals)
    {
        var sub = taxTotals.FirstOrDefault()?.TaxSubtotal.FirstOrDefault();
        return sub?.TaxCategory is not null
            ? (sub.TaxCategory.Id, sub.TaxCategory.Percent)
            : ("STANDARD_VAT", 7.5m);
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTimeOffset.TryParse(value, out var dto)) return DateOnly.FromDateTime(dto.UtcDateTime);
        if (DateOnly.TryParse(value, out var d)) return d;
        return null;
    }

    private static TimeOnly? ParseTimeOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TimeOnly.TryParse(value, out var t) ? t : null;
    }
}
