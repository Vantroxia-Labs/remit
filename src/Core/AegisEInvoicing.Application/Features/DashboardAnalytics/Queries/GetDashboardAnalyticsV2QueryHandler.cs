using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.DashboardAnalytics.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.DashboardAnalytics.Queries;

// Lightweight projection DTOs for optimized queries
internal record InvoiceLineProjection(
    DateTimeOffset CreatedAt,
    decimal Quantity,
    decimal UnitPrice,
    decimal? DiscountAmount,
    decimal? AdditionalFeeAmount,
    decimal TaxPercent,
    string CurrencyCode,
    string? Region,
    bool IsPaid
);

internal record ReceivedInvoiceProjection(
    DateTimeOffset CreatedAt,
    decimal PayableAmount,
    decimal TotalTaxAmount,
    string DocumentCurrencyCode
);

public class GetDashboardAnalyticsV2QueryHandler : IRequestHandler<GetDashboardAnalyticsV2Query, DashboardAnalyticsV2Dto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardAnalyticsV2QueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardAnalyticsV2Dto> Handle(GetDashboardAnalyticsV2Query request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var last12MonthsStart = today.AddMonths(-11).Date;
        var last12MonthsEnd = today.Date;

        return request.DashboardType switch
        {
            DashboardType.General => new DashboardAnalyticsV2Dto
            {
                GeneralDashboard = await GetGeneralDashboardData(last12MonthsStart, last12MonthsEnd, cancellationToken)
            },
            DashboardType.VATTable => new DashboardAnalyticsV2Dto
            {
                VATTableDashboard = await GetVATTableDashboardData(last12MonthsStart, last12MonthsEnd, cancellationToken)
            },
            _ => throw new ArgumentException("Invalid dashboard type", nameof(request.DashboardType))
        };
    }

    private async Task<GeneralDashboardDto> GetGeneralDashboardData(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var invoiceQuery = _context.Invoices.AsNoTracking().AsQueryable();
        var receivedInvoiceQuery = _context.ReceivedInvoices.AsNoTracking().AsQueryable();

        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
        {
            var businessId = _currentUserService.BusinessId.Value;
            invoiceQuery = invoiceQuery.Where(b => b.BusinessId == businessId);
            receivedInvoiceQuery = receivedInvoiceQuery.Where(r => r.BusinessId == businessId);
        }

        // Project only the fields we need - this is much faster than loading full entities
        var invoiceLines = await invoiceQuery
            .Where(i => i.CreatedAt.Date >= startDate && i.CreatedAt.Date <= endDate)
            .SelectMany(i => i.InvoiceLine.Select(line => new InvoiceLineProjection(
                i.CreatedAt,
                line.Quantity,
                line.BusinessItem.UnitPrice,
                line.DiscountFee != null ? (decimal?)line.DiscountFee.Amount : null,
                line.AdditionalFee != null ? (decimal?)line.AdditionalFee.Amount : null,
                line.BusinessItem.TaxCategory != null ? line.BusinessItem.TaxCategory.Percent : 0,
                i.Currency.Code,
                i.Party != null && i.Party.Address != null ? i.Party.Address.State : null,
                i.PaymentStatus == FIRSAccessPoint.Models.Enumerators.PaymentStatus.Paid
            )))
            .ToListAsync(cancellationToken);

        var receivedInvoices = await receivedInvoiceQuery
            .Where(r => r.CreatedAt.Date >= startDate && r.CreatedAt.Date <= endDate)
            .Select(r => new ReceivedInvoiceProjection(
                r.CreatedAt,
                r.PayableAmount,
                r.TotalTaxAmount,
                r.DocumentCurrencyCode
            ))
            .ToListAsync(cancellationToken);

        var metrics = CalculateMetrics(invoiceLines, receivedInvoices);
        var salesVsPurchases = CalculateSalesVsPurchases(invoiceLines, receivedInvoices, startDate);
        var vatTrendAnalysis = CalculateVATTrendAnalysis(invoiceLines, receivedInvoices, startDate);
        var salesAndPayment = CalculateSalesAndPayment(invoiceLines, startDate);
        var salesPerRegion = CalculateSalesPerRegion(invoiceLines, startDate);

        return new GeneralDashboardDto
        {
            Metrics = metrics,
            SalesVsPurchases = salesVsPurchases,
            VATTrendAnalysis = vatTrendAnalysis,
            SalesAndPaymentPerMonth = salesAndPayment,
            SalesPerRegion = salesPerRegion
        };
    }

    private async Task<VATTableDashboardDto> GetVATTableDashboardData(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var invoiceQuery = _context.Invoices.AsNoTracking().AsQueryable();
        var receivedInvoiceQuery = _context.ReceivedInvoices.AsNoTracking().AsQueryable();

        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
        {
            var businessId = _currentUserService.BusinessId.Value;
            invoiceQuery = invoiceQuery.Where(b => b.BusinessId == businessId);
            receivedInvoiceQuery = receivedInvoiceQuery.Where(r => r.BusinessId == businessId);
        }

        // Project only the fields we need
        var invoiceLines = await invoiceQuery
            .Where(i => i.CreatedAt.Date >= startDate && i.CreatedAt.Date <= endDate)
            .SelectMany(i => i.InvoiceLine.Select(line => new InvoiceLineProjection(
                i.CreatedAt,
                line.Quantity,
                line.BusinessItem.UnitPrice,
                line.DiscountFee != null ? (decimal?)line.DiscountFee.Amount : null,
                line.AdditionalFee != null ? (decimal?)line.AdditionalFee.Amount : null,
                line.BusinessItem.TaxCategory != null ? line.BusinessItem.TaxCategory.Percent : 0,
                i.Currency.Code,
                null,
                false
            )))
            .ToListAsync(cancellationToken);

        var receivedInvoices = await receivedInvoiceQuery
            .Where(r => r.CreatedAt.Date >= startDate && r.CreatedAt.Date <= endDate)
            .Select(r => new ReceivedInvoiceProjection(
                r.CreatedAt,
                r.PayableAmount,
                r.TotalTaxAmount,
                r.DocumentCurrencyCode
            ))
            .ToListAsync(cancellationToken);

        var vatTableByCurrency = CalculateVATTableByCurrency(invoiceLines, receivedInvoices, startDate);
        var exemptVATTableByCurrency = CalculateExemptVATTableByCurrency(invoiceLines, receivedInvoices, startDate);
        var vatTableVsNonVATTable = CalculateVATTableVsNonVATTable(invoiceLines, receivedInvoices, startDate);

        return new VATTableDashboardDto
        {
            VATTableByCurrency = vatTableByCurrency,
            ExemptVATTableByCurrency = exemptVATTableByCurrency,
            VATTableVsNonVATTableSalesAndPurchase = vatTableVsNonVATTable
        };
    }

    private static decimal CalculateLineTotal(InvoiceLineProjection line)
    {
        var lineTotal = Convert.ToDecimal(line.Quantity * line.UnitPrice);
        var discount = line.DiscountAmount ?? 0;
        var additionalFee = line.AdditionalFeeAmount ?? 0;
        return lineTotal - discount + additionalFee;
    }

    private static decimal CalculateLineVAT(InvoiceLineProjection line)
    {
        var lineTotal = CalculateLineTotal(line);
        return lineTotal * (Convert.ToDecimal(line.TaxPercent) / 100);
    }

    private static InvoiceSummaryMetricsDto CalculateMetrics(List<InvoiceLineProjection> invoiceLines, List<ReceivedInvoiceProjection> vendorInvoices)
    {
        var customerInvoicesCount = invoiceLines.Select(l => l.CreatedAt).Distinct().Count();
        var vendorInvoicesCount = vendorInvoices.Count;

        decimal totalCustomerAmount = invoiceLines.Sum(CalculateLineTotal);
        decimal totalVATOnCustomer = invoiceLines.Sum(CalculateLineVAT);

        var totalVendorAmount = vendorInvoices.Sum(v => v.PayableAmount);
        var totalVATOnVendor = vendorInvoices.Sum(v => v.TotalTaxAmount);

        var currentMonth = DateTime.Today;
        var previousMonth = currentMonth.AddMonths(-1);

        var currentMonthVATOnVendor = vendorInvoices
            .Where(v => v.CreatedAt.Year == currentMonth.Year && v.CreatedAt.Month == currentMonth.Month)
            .Sum(v => v.TotalTaxAmount);

        var previousMonthVATOnVendor = vendorInvoices
            .Where(v => v.CreatedAt.Year == previousMonth.Year && v.CreatedAt.Month == previousMonth.Month)
            .Sum(v => v.TotalTaxAmount);

        var currentMonthVATOnCustomer = invoiceLines
            .Where(l => l.CreatedAt.Year == currentMonth.Year && l.CreatedAt.Month == currentMonth.Month)
            .Sum(CalculateLineVAT);

        var previousMonthVATOnCustomer = invoiceLines
            .Where(l => l.CreatedAt.Year == previousMonth.Year && l.CreatedAt.Month == previousMonth.Month)
            .Sum(CalculateLineVAT);

        var currentMonthTotal = invoiceLines
            .Where(l => l.CreatedAt.Year == currentMonth.Year && l.CreatedAt.Month == currentMonth.Month)
            .Sum(CalculateLineTotal) +
            vendorInvoices
            .Where(v => v.CreatedAt.Year == currentMonth.Year && v.CreatedAt.Month == currentMonth.Month)
            .Sum(v => v.PayableAmount);

        var previousMonthTotal = invoiceLines
            .Where(l => l.CreatedAt.Year == previousMonth.Year && l.CreatedAt.Month == previousMonth.Month)
            .Sum(CalculateLineTotal) +
            vendorInvoices
            .Where(v => v.CreatedAt.Year == previousMonth.Year && v.CreatedAt.Month == previousMonth.Month)
            .Sum(v => v.PayableAmount);

        var vatOnVendorChange = previousMonthVATOnVendor != 0
            ? Math.Round((currentMonthVATOnVendor - previousMonthVATOnVendor) / previousMonthVATOnVendor * 100, 2)
            : 0;

        var vatOnCustomerChange = previousMonthVATOnCustomer != 0
            ? Math.Round((currentMonthVATOnCustomer - previousMonthVATOnCustomer) / previousMonthVATOnCustomer * 100, 2)
            : 0;

        var totalInvoiceValueChange = previousMonthTotal != 0
            ? Math.Round((currentMonthTotal - previousMonthTotal) / previousMonthTotal * 100, 2)
            : 0;

        return new InvoiceSummaryMetricsDto
        {
            TotalCustomerInvoicesCount = customerInvoicesCount,
            TotalCustomerInvoicesAmount = totalCustomerAmount,
            TotalVendorInvoicesCount = vendorInvoicesCount,
            TotalVendorInvoicesAmount = totalVendorAmount,
            TotalVATOnVendorInvoices = totalVATOnVendor,
            TotalVATOnCustomerInvoices = totalVATOnCustomer,
            TotalInvoiceValue = totalCustomerAmount + totalVendorAmount,
            VATOnVendorPercentageChange = vatOnVendorChange,
            VATOnCustomerPercentageChange = vatOnCustomerChange,
            TotalInvoiceValuePercentageChange = totalInvoiceValueChange
        };
    }

    private static List<SalesVsPurchasesMonthlyDto> CalculateSalesVsPurchases(
        List<InvoiceLineProjection> invoiceLines,
        List<ReceivedInvoiceProjection> vendorInvoices,
        DateTime startDate)
    {
        return Enumerable.Range(0, 12)
            .Select(i =>
            {
                var monthDate = startDate.AddMonths(i);
                var year = monthDate.Year;
                var month = monthDate.Month;

                var salesAmount = invoiceLines
                    .Where(l => l.CreatedAt.Year == year && l.CreatedAt.Month == month)
                    .Sum(CalculateLineTotal);

                var purchasesAmount = vendorInvoices
                    .Where(v => v.CreatedAt.Year == year && v.CreatedAt.Month == month)
                    .Sum(v => v.PayableAmount);

                return new SalesVsPurchasesMonthlyDto
                {
                    Year = year,
                    Month = month,
                    MonthName = monthDate.ToString("MMM"),
                    Name = monthDate.ToString("MMM yyyy"),
                    SalesAmount = salesAmount,
                    PurchasesAmount = purchasesAmount
                };
            })
            .ToList();
    }

    private static List<VATTrendAnalysisMonthlyDto> CalculateVATTrendAnalysis(
        List<InvoiceLineProjection> invoiceLines,
        List<ReceivedInvoiceProjection> vendorInvoices,
        DateTime startDate)
    {
        return Enumerable.Range(0, 12)
            .Select(i =>
            {
                var monthDate = startDate.AddMonths(i);
                var year = monthDate.Year;
                var month = monthDate.Month;

                var outputVAT = invoiceLines
                    .Where(l => l.CreatedAt.Year == year && l.CreatedAt.Month == month)
                    .Sum(CalculateLineVAT);

                var inputVAT = vendorInvoices
                    .Where(v => v.CreatedAt.Year == year && v.CreatedAt.Month == month)
                    .Sum(v => v.TotalTaxAmount);

                return new VATTrendAnalysisMonthlyDto
                {
                    Year = year,
                    Month = month,
                    MonthName = monthDate.ToString("MMM"),
                    Name = monthDate.ToString("MMM yyyy"),
                    InputVAT = inputVAT,
                    OutputVAT = outputVAT
                };
            })
            .ToList();
    }

    private static List<SalesAndPaymentMonthlyDto> CalculateSalesAndPayment(
        List<InvoiceLineProjection> invoiceLines,
        DateTime startDate)
    {
        return Enumerable.Range(0, 12)
            .Select(i =>
            {
                var monthDate = startDate.AddMonths(i);
                var year = monthDate.Year;
                var month = monthDate.Month;

                var monthlyLines = invoiceLines
                    .Where(l => l.CreatedAt.Year == year && l.CreatedAt.Month == month)
                    .ToList();

                var sales = monthlyLines.Sum(CalculateLineTotal);
                var payment = monthlyLines.Where(l => l.IsPaid).Sum(CalculateLineTotal);

                return new SalesAndPaymentMonthlyDto
                {
                    Year = year,
                    Month = month,
                    MonthName = monthDate.ToString("MMM"),
                    Name = monthDate.ToString("MMM yyyy"),
                    Sales = sales,
                    Payment = payment
                };
            })
            .ToList();
    }

    private static List<SalesPerRegionMonthlyDto> CalculateSalesPerRegion(
        List<InvoiceLineProjection> invoiceLines,
        DateTime startDate)
    {
        var monthlyData = new List<SalesPerRegionMonthlyDto>();

        for (int i = 0; i < 12; i++)
        {
            var monthDate = startDate.AddMonths(i);
            var year = monthDate.Year;
            var month = monthDate.Month;

            var regionGroups = invoiceLines
                .Where(l => l.CreatedAt.Year == year && l.CreatedAt.Month == month)
                .GroupBy(l => l.Region ?? "Unknown")
                .Select(g => new SalesPerRegionMonthlyDto
                {
                    Year = year,
                    Month = month,
                    MonthName = monthDate.ToString("MMM"),
                    Name = monthDate.ToString("MMM yyyy"),
                    Region = g.Key,
                    SalesAmount = g.Sum(CalculateLineTotal)
                })
                .ToList();

            monthlyData.AddRange(regionGroups);
        }

        return monthlyData;
    }

    private static List<VATTableByCurrencyMonthlyDto> CalculateVATTableByCurrency(
        List<InvoiceLineProjection> invoiceLines,
        List<ReceivedInvoiceProjection> vendorInvoices,
        DateTime startDate)
    {
        return Enumerable.Range(0, 12)
            .Select(i =>
            {
                var monthDate = startDate.AddMonths(i);
                var year = monthDate.Year;
                var month = monthDate.Month;

                var customerVATByCurrency = invoiceLines
                    .Where(l => l.CreatedAt.Year == year && l.CreatedAt.Month == month)
                    .GroupBy(l => l.CurrencyCode)
                    .ToDictionary(g => g.Key, g => g.Sum(CalculateLineVAT));

                var vendorVATByCurrency = vendorInvoices
                    .Where(v => v.CreatedAt.Year == year && v.CreatedAt.Month == month)
                    .GroupBy(v => v.DocumentCurrencyCode)
                    .ToDictionary(g => g.Key, g => g.Sum(v => v.TotalTaxAmount));

                var currencyAmounts = new List<CurrencyAmountDto>();

                foreach (var currencyCode in Enum.GetValues<CurrencyCode>())
                {
                    var code = currencyCode.ToString();
                    var customerAmount = customerVATByCurrency.GetValueOrDefault(code, 0);
                    var vendorAmount = vendorVATByCurrency.GetValueOrDefault(code, 0);
                    var totalAmount = customerAmount + vendorAmount;

                    currencyAmounts.Add(new CurrencyAmountDto
                    {
                        Currency = currencyCode,
                        CurrencyName = code,
                        Amount = totalAmount
                    });
                }

                return new VATTableByCurrencyMonthlyDto
                {
                    Year = year,
                    Month = month,
                    MonthName = monthDate.ToString("MMM"),
                    Name = monthDate.ToString("MMM yyyy"),
                    CurrencyAmounts = currencyAmounts
                };
            })
            .ToList();
    }

    private static List<ExemptVATTableByCurrencyMonthlyDto> CalculateExemptVATTableByCurrency(
        List<InvoiceLineProjection> invoiceLines,
        List<ReceivedInvoiceProjection> vendorInvoices,
        DateTime startDate)
    {
        return Enumerable.Range(0, 12)
            .Select(i =>
            {
                var monthDate = startDate.AddMonths(i);
                var year = monthDate.Year;
                var month = monthDate.Month;

                var customerExemptByCurrency = invoiceLines
                    .Where(l => l.CreatedAt.Year == year && l.CreatedAt.Month == month && l.TaxPercent == 0)
                    .GroupBy(l => l.CurrencyCode)
                    .ToDictionary(g => g.Key, g => g.Sum(CalculateLineTotal));

                var vendorExemptByCurrency = vendorInvoices
                    .Where(v => v.CreatedAt.Year == year && v.CreatedAt.Month == month && v.TotalTaxAmount == 0)
                    .GroupBy(v => v.DocumentCurrencyCode)
                    .ToDictionary(g => g.Key, g => g.Sum(v => v.PayableAmount));

                var currencyAmounts = new List<CurrencyAmountDto>();

                foreach (var currencyCode in Enum.GetValues<CurrencyCode>())
                {
                    var code = currencyCode.ToString();
                    var customerAmount = customerExemptByCurrency.GetValueOrDefault(code, 0);
                    var vendorAmount = vendorExemptByCurrency.GetValueOrDefault(code, 0);
                    var totalAmount = customerAmount + vendorAmount;

                    currencyAmounts.Add(new CurrencyAmountDto
                    {
                        Currency = currencyCode,
                        CurrencyName = code,
                        Amount = totalAmount
                    });
                }

                return new ExemptVATTableByCurrencyMonthlyDto
                {
                    Year = year,
                    Month = month,
                    MonthName = monthDate.ToString("MMM"),
                    Name = monthDate.ToString("MMM yyyy"),
                    CurrencyAmounts = currencyAmounts
                };
            })
            .ToList();
    }

    private static List<VATTableVsNonVATTableMonthlyDto> CalculateVATTableVsNonVATTable(
        List<InvoiceLineProjection> invoiceLines,
        List<ReceivedInvoiceProjection> vendorInvoices,
        DateTime startDate)
    {
        return Enumerable.Range(0, 12)
            .Select(i =>
            {
                var monthDate = startDate.AddMonths(i);
                var year = monthDate.Year;
                var month = monthDate.Month;

                var monthlyLines = invoiceLines
                    .Where(l => l.CreatedAt.Year == year && l.CreatedAt.Month == month)
                    .ToList();

                var monthlyVendorInvoices = vendorInvoices
                    .Where(v => v.CreatedAt.Year == year && v.CreatedAt.Month == month)
                    .ToList();

                var salesVatable = monthlyLines.Where(l => l.TaxPercent > 0).Sum(CalculateLineTotal);
                var salesNonVatable = monthlyLines.Where(l => l.TaxPercent == 0).Sum(CalculateLineTotal);

                var purchaseVatable = monthlyVendorInvoices.Where(v => v.TotalTaxAmount > 0).Sum(v => v.PayableAmount);
                var purchaseNonVatable = monthlyVendorInvoices.Where(v => v.TotalTaxAmount == 0).Sum(v => v.PayableAmount);

                return new VATTableVsNonVATTableMonthlyDto
                {
                    Year = year,
                    Month = month,
                    MonthName = monthDate.ToString("MMM"),
                    Name = monthDate.ToString("MMM yyyy"),
                    SalesVatable = salesVatable,
                    SalesNonVatable = salesNonVatable,
                    PurchaseVatable = purchaseVatable,
                    PurchaseNonVatable = purchaseNonVatable
                };
            })
            .ToList();
    }
}
