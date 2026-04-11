using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Commands.GenerateVatSchedule;

public class GenerateVatScheduleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GenerateVatScheduleCommandHandler> logger)
    : IRequestHandler<GenerateVatScheduleCommand, VatScheduleDto>
{
    private static readonly InvoiceStatus[] EligibleStatuses =
    [
        InvoiceStatus.TRANSMITTED,
        InvoiceStatus.ACKNOWLEDGED,
        InvoiceStatus.COMPLETELYTRANSMITTED,
    ];

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
                vat += 0m; // TaxCategory removed from BusinessItem
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

        // ── Mark invoices as scheduled so they cant be double-counted ────────
        // Load tracked copies to mutate
        var trackedInvoices = await context.Invoices
            .Where(i => invoiceData.Select(x => x.Id).Contains(i.Id))
            .ToListAsync(cancellationToken);

        foreach (var inv in trackedInvoices)
            inv.AssignToVatSchedule(schedule.Id);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "VAT schedule {ScheduleId} generated for {Year}-{Month} with {Count} invoices.",
            schedule.Id, request.Year, request.Month, items.Count);

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
        };
    }
}
