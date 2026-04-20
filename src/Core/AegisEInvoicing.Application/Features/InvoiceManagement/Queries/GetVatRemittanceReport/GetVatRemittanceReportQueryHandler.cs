using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetVatRemittanceReport;

public class GetVatRemittanceReportQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<GetVatRemittanceReportQuery, VatRemittanceReportDto>
{
    private static readonly InvoiceStatus[] RemittableStatuses =
    [
        InvoiceStatus.TRANSMITTED,
        InvoiceStatus.ACKNOWLEDGED,
    ];

    public async Task<VatRemittanceReportDto> Handle(GetVatRemittanceReportQuery request, CancellationToken cancellationToken)
    {
        var query = context.Invoices
            .AsNoTracking()
            .Where(i => !i.IsDeleted && RemittableStatuses.Contains(i.InvoiceStatus))
            .Where(i => i.IssueDate >= request.StartDate && i.IssueDate <= request.EndDate);

        if (!currentUser.IsPlatformAdmin && currentUser.BusinessId.HasValue)
            query = query.Where(i => i.BusinessId == currentUser.BusinessId.Value);

        // Project the data we need per invoice line
        var lines = await query
            .SelectMany(i => i.InvoiceLine.Select(line => new
            {
                Year = i.IssueDate.Year,
                Month = i.IssueDate.Month,
                InvoiceId = i.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPriceSnapshot,
                DiscountAmount = line.DiscountFee != null ? (decimal?)line.DiscountFee.Amount : null,
                AdditionalFeeAmount = line.AdditionalFee != null ? (decimal?)line.AdditionalFee.Amount : null,
                TaxPercent = 0m,
            }))
            .ToListAsync(cancellationToken);

        var periods = lines
            .GroupBy(l => new { l.Year, l.Month })
            .Select(g =>
            {
                var invoiceCount = g.Select(l => l.InvoiceId).Distinct().Count();
                var taxable = 0m;
                var vat = 0m;

                foreach (var l in g)
                {
                    var lineTotal = l.Quantity * l.UnitPrice
                        - (l.DiscountAmount ?? 0m)
                        + (l.AdditionalFeeAmount ?? 0m);
                    taxable += lineTotal;
                    vat += lineTotal * l.TaxPercent / 100m;
                }

                return new VatRemittancePeriodDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    InvoiceCount = invoiceCount,
                    TaxableAmount = Math.Round(taxable, 2),
                    VatAmount = Math.Round(vat, 2),
                };
            })
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .ToList();

        return new VatRemittanceReportDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Periods = periods,
            TotalTaxableAmount = periods.Sum(p => p.TaxableAmount),
            TotalVatAmount = periods.Sum(p => p.VatAmount),
            TotalInvoiceCount = periods.Sum(p => p.InvoiceCount),
        };
    }
}
