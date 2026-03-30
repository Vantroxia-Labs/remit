using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.DashboardAnalytics.DTOs;

public record DashboardAnalyticsV2Dto
{
    public GeneralDashboardDto? GeneralDashboard { get; init; }
    public VATTableDashboardDto? VATTableDashboard { get; init; }
}

public record GeneralDashboardDto
{
    public InvoiceSummaryMetricsDto Metrics { get; init; } = null!;
    public List<SalesVsPurchasesMonthlyDto> SalesVsPurchases { get; init; } = [];
    public List<VATTrendAnalysisMonthlyDto> VATTrendAnalysis { get; init; } = [];
    public List<SalesAndPaymentMonthlyDto> SalesAndPaymentPerMonth { get; init; } = [];
    public List<SalesPerRegionMonthlyDto> SalesPerRegion { get; init; } = [];
}

public record VATTableDashboardDto
{
    public List<VATTableByCurrencyMonthlyDto> VATTableByCurrency { get; init; } = [];
    public List<ExemptVATTableByCurrencyMonthlyDto> ExemptVATTableByCurrency { get; init; } = [];
    public List<VATTableVsNonVATTableMonthlyDto> VATTableVsNonVATTableSalesAndPurchase { get; init; } = [];
}

public record InvoiceSummaryMetricsDto
{
    public int TotalCustomerInvoicesCount { get; init; }
    public decimal TotalCustomerInvoicesAmount { get; init; }
    public int TotalVendorInvoicesCount { get; init; }
    public decimal TotalVendorInvoicesAmount { get; init; }
    public decimal TotalVATOnVendorInvoices { get; init; }
    public decimal TotalVATOnCustomerInvoices { get; init; }
    public decimal TotalInvoiceValue { get; init; }
    public decimal VATOnVendorPercentageChange { get; init; }
    public decimal VATOnCustomerPercentageChange { get; init; }
    public decimal TotalInvoiceValuePercentageChange { get; init; }
}

public record SalesVsPurchasesMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal SalesAmount { get; init; }
    public decimal PurchasesAmount { get; init; }
}

public record VATTrendAnalysisMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal InputVAT { get; init; }
    public decimal OutputVAT { get; init; }
}

public record SalesAndPaymentMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Sales { get; init; }
    public decimal Payment { get; init; }
}

public record SalesPerRegionMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public decimal SalesAmount { get; init; }
}

public record VATTableByCurrencyMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<CurrencyAmountDto> CurrencyAmounts { get; init; } = [];
}

public record ExemptVATTableByCurrencyMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<CurrencyAmountDto> CurrencyAmounts { get; init; } = [];
}

public record CurrencyAmountDto
{
    public CurrencyCode Currency { get; init; }
    public string CurrencyName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public record VATTableVsNonVATTableMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal SalesVatable { get; init; }
    public decimal SalesNonVatable { get; init; }
    public decimal PurchaseVatable { get; init; }
    public decimal PurchaseNonVatable { get; init; }
}
