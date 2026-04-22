using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Commands.GenerateVatSchedule;

public class GenerateVatScheduleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GenerateVatScheduleCommandHandler> logger)
    : IRequestHandler<GenerateVatScheduleCommand, VatScheduleDto>
{
    private static readonly InvoiceStatus[] EligibleStatuses =
    [
        InvoiceStatus.SIGNED,
        InvoiceStatus.TRANSMITTED,
        InvoiceStatus.ACKNOWLEDGED,
    ];

    private static readonly HashSet<string> VatCategoryIds =
        ["STANDARD_VAT", "REDUCED_VAT"];

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public async Task<VatScheduleDto> Handle(GenerateVatScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.BusinessId.HasValue)
            throw new BadRequestException("Business context is required to generate a VAT schedule.");

        var businessId = currentUser.BusinessId.Value;

        // ── Guard: only one schedule per business per month ──────────────────
        var exists = await context.VatSchedules
            .AnyAsync(s => s.BusinessId == businessId
                        && s.Year == request.Year
                        && s.Month == request.Month,
                      cancellationToken);

        if (exists)
            throw new ConflictException(
                $"A VAT schedule already exists for {request.Month:D2}/{request.Year}. " +
                "Only one schedule is allowed per month.");

        // ── Validate period ──────────────────────────────────────────────────
        var periodStart = new DateOnly(request.Year, request.Month, 1);
        var periodEnd = new DateOnly(request.Year, request.Month,
            DateTime.DaysInMonth(request.Year, request.Month));

        // ── Fetch eligible invoices ──────────────────────────────────────────
        var invoiceData = await context.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceLine)
                .ThenInclude(l => l.BusinessItem)
            .Include(i => i.Party)
            .Where(i => !i.IsDeleted
                     && i.BusinessId == businessId
                     && i.VatScheduleId == null                   // not yet in any schedule
                     && EligibleStatuses.Contains(i.InvoiceStatus)
                     && i.IssueDate >= periodStart
                     && i.IssueDate <= periodEnd)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Generating VAT schedule {Year}-{Month} for business {BusinessId}: {Count} eligible invoices.",
            request.Year, request.Month, businessId, invoiceData.Count);

        // ── Create the schedule ──────────────────────────────────────────────
        var schedule = VatSchedule.Create(businessId, request.Year, request.Month);
        context.VatSchedules.Add(schedule);

        // ── Flush to get the schedule's Id ───────────────────────────────────
        await context.SaveChangesAsync(cancellationToken);

        // ── Build items ──────────────────────────────────────────────────────
        var items = new List<VatScheduleItem>();
        foreach (var invoice in invoiceData)
        {
            decimal taxable = 0m, vat = 0m;
            foreach (var line in invoice.InvoiceLine)
            {
                var lineTotal = line.Quantity * line.UnitPriceSnapshot
                    - (line.DiscountFee?.Amount ?? 0m)
                    + (line.AdditionalFee?.Amount ?? 0m);
                taxable += lineTotal;
                vat += line.BusinessItem?.TaxCategories.Sum(tc => tc.CalculateTax(lineTotal)) ?? 0m;
            }

            items.Add(VatScheduleItem.Create(
                scheduleId: schedule.Id,
                invoiceId: invoice.Id,
                invoiceCode: invoice.InvoiceCode,
                irn: invoice.Irn?.Value,
                partyName: invoice.Party.Name,
                partyTin: invoice.Party.TaxIdentificationNumber?.Value,
                issueDate: invoice.IssueDate,
                taxableAmount: Math.Round(taxable, 2),
                vatAmount: Math.Round(vat, 2),
                paymentStatus: invoice.PaymentStatus));
        }

        schedule.AddItems(items);
        context.VatScheduleItems.AddRange(items);

        // ── Mark output invoices as scheduled so they can't be double-counted ─
        var trackedInvoices = await context.Invoices
            .Where(i => invoiceData.Select(x => x.Id).Contains(i.Id))
            .ToListAsync(cancellationToken);

        foreach (var inv in trackedInvoices)
            inv.AssignToVatSchedule(schedule.Id);

        // ── Fetch eligible received invoices (input VAT) ──────────────────────
        var receivedInvoiceData = await context.ReceivedInvoices
            .AsNoTracking()
            .Where(r => !r.IsDeleted
                     && r.BusinessId == businessId
                     && r.InputVatScheduleId == null
                     && r.IssueDate >= periodStart
                     && r.IssueDate <= periodEnd)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "VAT schedule {ScheduleId}: {Count} received invoices eligible for input VAT.",
            schedule.Id, receivedInvoiceData.Count);

        var inputItems = new List<InputVatScheduleItem>();
        foreach (var ri in receivedInvoiceData)
        {
            if (string.IsNullOrWhiteSpace(ri.TaxTotalJson))
                continue;

            List<AppTaxTotal>? taxTotals = null;
            try
            {
                taxTotals = JsonSerializer.Deserialize<List<AppTaxTotal>>(
                    ri.TaxTotalJson, JsonOpts);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex,
                    "Could not deserialize TaxTotalJson for ReceivedInvoice {Id}; skipping input VAT.",
                    ri.Id);
                continue;
            }

            if (taxTotals is null || taxTotals.Count == 0)
                continue;

            var vatTaxable = taxTotals
                .Where(t => t.TaxCategoryId != null
                         && VatCategoryIds.Contains(t.TaxCategoryId))
                .Sum(t => t.TaxableAmount);

            var vatAmount = taxTotals
                .Where(t => t.TaxCategoryId != null
                         && VatCategoryIds.Contains(t.TaxCategoryId))
                .Sum(t => t.TaxAmount);

            inputItems.Add(InputVatScheduleItem.Create(
                scheduleId: schedule.Id,
                receivedInvoiceId: ri.Id,
                irn: ri.Irn.Value,
                supplierName: ri.SupplierPartyName,
                supplierTin: ri.SupplierTIN?.Value,
                issueDate: ri.IssueDate,
                taxableAmount: Math.Round(vatTaxable, 2),
                vatAmount: Math.Round(vatAmount, 2)));
        }

        schedule.AddInputItems(inputItems);
        context.InputVatScheduleItems.AddRange(inputItems);

        // ── Mark received invoices so they can't be double-counted ─────────────
        var trackedReceived = await context.ReceivedInvoices
            .Where(r => receivedInvoiceData.Select(x => x.Id).Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var ri in trackedReceived)
            ri.AssignToInputVatSchedule(schedule.Id);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "VAT schedule {ScheduleId} generated for {Year}-{Month}: {OutputCount} output invoices, {InputCount} input invoices.",
            schedule.Id, request.Year, request.Month, items.Count, inputItems.Count);

        return new VatScheduleDto
        {
            Id = schedule.Id,
            Year = schedule.Year,
            Month = schedule.Month,
            MonthName = schedule.MonthName,
            PeriodStart = schedule.PeriodStart,
            PeriodEnd = schedule.PeriodEnd,
            DueDate = schedule.DueDate,
            Status = schedule.Status.ToString(),
            FiledAt = schedule.FiledAt,
            GeneratedAt = schedule.CreatedAt,
            TotalInvoiceCount = schedule.TotalInvoiceCount,
            TotalTaxableAmount = schedule.TotalTaxableAmount,
            TotalVatAmount = schedule.TotalVatAmount,
            TotalInputInvoiceCount = schedule.TotalInputInvoiceCount,
            TotalInputTaxableAmount = schedule.TotalInputTaxableAmount,
            TotalInputVatAmount = schedule.TotalInputVatAmount,
            NetVatPayable = schedule.NetVatPayable,
            Items = items.Select(i => new VatScheduleItemDto
            {
                Id = i.Id,
                InvoiceId = i.InvoiceId,
                InvoiceCode = i.InvoiceCode,
                Irn = i.Irn,
                PartyName = i.PartyName,
                PartyTin = i.PartyTin,
                IssueDate = i.IssueDate,
                TaxableAmount = i.TaxableAmount,
                VatAmount = i.VatAmount,
                TotalAmount = i.TotalAmount,
                PaymentStatus = i.PaymentStatus.ToString(),
            }).ToList(),
            InputItems = inputItems.Select(i => new InputVatScheduleItemDto
            {
                Id = i.Id,
                ReceivedInvoiceId = i.ReceivedInvoiceId,
                Irn = i.Irn,
                SupplierName = i.SupplierName,
                SupplierTin = i.SupplierTin,
                IssueDate = i.IssueDate,
                TaxableAmount = i.TaxableAmount,
                VatAmount = i.VatAmount,
                TotalAmount = i.TotalAmount,
            }).ToList(),
        };
    }
}
