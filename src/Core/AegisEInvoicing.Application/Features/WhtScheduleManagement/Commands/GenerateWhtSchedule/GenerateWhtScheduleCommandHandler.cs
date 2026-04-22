using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Commands.GenerateWhtSchedule;

public class GenerateWhtScheduleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GenerateWhtScheduleCommandHandler> logger)
    : IRequestHandler<GenerateWhtScheduleCommand, WhtScheduleDto>
{
    private const string WhtCategoryId = "Withholding_Tax";
    private const decimal DefaultWhtRate = 5m; // default for services (companies)
    private static readonly WhtNatureOfTransaction DefaultNature = WhtNatureOfTransaction.Services;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<WhtScheduleDto> Handle(GenerateWhtScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.BusinessId.HasValue)
            throw new BadRequestException("Business context is required to generate a WHT schedule.");

        var businessId = currentUser.BusinessId.Value;

        // ── Guard: one schedule per business per month ───────────────────────
        var exists = await context.WhtSchedules
            .AnyAsync(s => s.BusinessId == businessId
                        && s.Year == request.Year
                        && s.Month == request.Month,
                      cancellationToken);

        if (exists)
            throw new ConflictException(
                $"A WHT schedule already exists for {request.Month:D2}/{request.Year}. " +
                "Only one schedule is allowed per month.");

        var periodStart = new DateOnly(request.Year, request.Month, 1);
        var periodEnd = new DateOnly(request.Year, request.Month,
            DateTime.DaysInMonth(request.Year, request.Month));

        // ── Fetch eligible received invoices (B2B and B2G only — WHT applies) ─
        // B2C (InvoiceTypeCode == "B2C") → individual payee → State IRS
        // B2B / B2G → company/government payee → NRS
        // We include ALL for the schedule but tag each by authority
        var receivedInvoices = await context.ReceivedInvoices
            .AsNoTracking()
            .Where(r => !r.IsDeleted
                     && r.BusinessId == businessId
                     && r.WhtScheduleId == null
                     && r.IssueDate >= periodStart
                     && r.IssueDate <= periodEnd)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Generating WHT schedule {Year}-{Month} for business {BusinessId}: {Count} eligible received invoices.",
            request.Year, request.Month, businessId, receivedInvoices.Count);

        // ── Create the schedule ──────────────────────────────────────────────
        var schedule = WhtSchedule.Create(businessId, request.Year, request.Month);
        context.WhtSchedules.Add(schedule);
        await context.SaveChangesAsync(cancellationToken); // get the schedule Id

        // ── Build items ──────────────────────────────────────────────────────
        var items = new List<WhtScheduleItem>();

        foreach (var ri in receivedInvoices)
        {
            // Determine tax authority from invoice type code
            var authority = ri.InvoiceTypeCode?.Equals("B2C", StringComparison.OrdinalIgnoreCase) == true
                ? WhtTaxAuthority.StateIRS
                : WhtTaxAuthority.NRS;

            // Try to extract WHT from TaxTotalJson (NRS MBS Feb-2026 schema supports Withholding_Tax category)
            decimal whtRate = DefaultWhtRate;
            WhtNatureOfTransaction nature = DefaultNature;

            if (!string.IsNullOrWhiteSpace(ri.TaxTotalJson))
            {
                try
                {
                    var taxTotals = JsonSerializer.Deserialize<List<AppTaxTotal>>(ri.TaxTotalJson, JsonOpts);
                    var whtTax = taxTotals?
                        .FirstOrDefault(t => t.TaxCategoryId?
                            .Equals(WhtCategoryId, StringComparison.OrdinalIgnoreCase) == true);

                    if (whtTax?.Percent.HasValue == true && whtTax.Percent.Value > 0)
                        whtRate = whtTax.Percent.Value;
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex,
                        "Could not deserialize TaxTotalJson for ReceivedInvoice {Id}; using default WHT rate.",
                        ri.Id);
                }
            }

            // Supply address from supplier data (can be null)
            var vendorAddress = ri.SupplierAddress is not null
                ? $"{ri.SupplierAddress.Street}, {ri.SupplierAddress.City}, {ri.SupplierAddress.State}".Trim(' ', ',')
                : null;

            items.Add(WhtScheduleItem.Create(
                scheduleId: schedule.Id,
                receivedInvoiceId: ri.Id,
                vendorName: ri.SupplierPartyName,
                vendorAddress: vendorAddress,
                vendorTin: ri.SupplierTIN?.Value,
                irn: ri.Irn.Value,
                issueDate: ri.IssueDate,
                natureOfTransaction: nature,
                grossAmount: ri.PayableAmount,
                whtRate: whtRate,
                taxAuthority: authority));
        }

        schedule.AddItems(items);
        context.WhtScheduleItems.AddRange(items);

        // ── Stamp received invoices to prevent double-counting ───────────────
        var trackedReceived = await context.ReceivedInvoices
            .Where(r => receivedInvoices.Select(x => x.Id).Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var ri in trackedReceived)
            ri.AssignToWhtSchedule(schedule.Id);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WHT schedule {ScheduleId} generated for {Year}-{Month}: {Count} items. NRS: {NrsAmount:N2}, State: {StateAmount:N2}.",
            schedule.Id, request.Year, request.Month, items.Count,
            schedule.TotalNrsWhtAmount, schedule.TotalStateWhtAmount);

        return MapToDto(schedule, items);
    }

    private static WhtScheduleDto MapToDto(WhtSchedule schedule, List<WhtScheduleItem> items) => new()
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
        TotalItemCount = schedule.TotalItemCount,
        TotalGrossAmount = schedule.TotalGrossAmount,
        TotalWhtAmount = schedule.TotalWhtAmount,
        TotalNrsWhtAmount = schedule.TotalNrsWhtAmount,
        TotalStateWhtAmount = schedule.TotalStateWhtAmount,
        Items = items.Select(i => new WhtScheduleItemDto
        {
            Id = i.Id,
            ReceivedInvoiceId = i.ReceivedInvoiceId,
            VendorName = i.VendorName,
            VendorAddress = i.VendorAddress,
            VendorTin = i.VendorTin,
            Irn = i.Irn,
            IssueDate = i.IssueDate,
            NatureOfTransaction = i.NatureOfTransaction.ToString(),
            GrossAmount = i.GrossAmount,
            WhtRate = i.WhtRate,
            WhtAmount = i.WhtAmount,
            NetAmount = i.NetAmount,
            TaxAuthority = i.TaxAuthority.ToString(),
        }).ToList(),
    };
}
