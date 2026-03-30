namespace AegisEInvoicing.Application.Features.DashboardAnalytics.DTOs;

public record DashboardAnalyticsDto(
    int TotalInvoices,
    int DraftInvoices,
    int SubmittedInvoices,
    int ApprovedInvoices,
    int RejectedInvoices,
    int PaidInvoices,
    decimal TotalInvoiceAmount,
    double AverageProcessingTimeDays,
    decimal SuccessRatePercentage,
    decimal RejectionRatePercentage,
    List<MonthlySalesDataDto> MonthlySales,
    List<MonthlyPurchasesDataDto> MonthlyPurchases,
    MonthlyComparisonDto MonthlyComparison);

public record MonthlySalesDataDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public decimal SalesAmount { get; init; }

    public MonthlySalesDataDto() { }

    public MonthlySalesDataDto(int year, int month, string monthName, decimal salesAmount)
    {
        Year = year;
        Month = month;
        MonthName = monthName;
        SalesAmount = salesAmount;
    }
}

public record MonthlyPurchasesDataDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public decimal PurchasesAmount { get; init; }

    public MonthlyPurchasesDataDto() { }

    public MonthlyPurchasesDataDto(int year, int month, string monthName, decimal purchasesAmount)
    {
        Year = year;
        Month = month;
        MonthName = monthName;
        PurchasesAmount = purchasesAmount;
    }
}

public record MonthlyComparisonDto
{
    public decimal CurrentMonthSales { get; init; }
    public decimal CurrentMonthPurchases { get; init; }
    public decimal SalesPercentageChange { get; init; }
    public decimal PurchasesPercentageChange { get; init; }
    public string SalesComparisonString { get; init; } = string.Empty;

    public MonthlyComparisonDto() { }

    public MonthlyComparisonDto(
        decimal currentMonthSales,
        decimal currentMonthPurchases,
        decimal salesPercentageChange,
        decimal purchasesPercentageChange,
        string salesComparisonString)
    {
        CurrentMonthSales = currentMonthSales;
        CurrentMonthPurchases = currentMonthPurchases;
        SalesPercentageChange = salesPercentageChange;
        PurchasesPercentageChange = purchasesPercentageChange;
        SalesComparisonString = salesComparisonString;
    }
}