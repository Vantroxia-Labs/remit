using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.DashboardAnalytics.DTOs;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.DashboardAnalytics.Queries;

public class GetDashboardAnalyticsQueryHandler : IRequestHandler<GetDashboardAnalyticsQuery, DashboardAnalyticsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardAnalyticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardAnalyticsDto> Handle(GetDashboardAnalyticsQuery request, CancellationToken cancellationToken)
    {
        DateTime today = DateTime.Today;
        DateTime startDate, endDate;

        if (request.ThisWeek)
            (startDate, endDate) = GetWeekRange(today);
        else
            (startDate, endDate) = GetMonthRange(today);

        // Get date range for monthly data (last 12 months)
        var monthlyStartDate = today.AddMonths(-11).Date; // Last 12 months including current
        var monthlyEndDate = today.Date;

        decimal totalSalesAmount = 0.00M;
        decimal totalPurchasesAmount = 0.00M;

        // Build base query with security filter
        var query = _context.Invoices
            .Include(x => x.InvoiceLine)
            .ThenInclude(b => b.BusinessItem)
            .AsNoTracking()
            .AsQueryable();

        // Apply security filters - Business admins can only see their own business statistics
        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            query = query.Where(b => b.BusinessId == _currentUserService.BusinessId!.Value);

        var filteredQuery = query.Where(i => i.CreatedAt.Date >= startDate.Date && i.CreatedAt.Date < endDate.AddDays(1).Date);

        // Single optimized query using GroupBy aggregation
        var stats = await query
            .GroupBy(i => 1)
            .Select(g => new
            {
                TotalInvoices = g.Count(),
                DraftInvoices = g.Count(i => i.InvoiceStatus == Domain.Enums.InvoiceStatus.DRAFT),
                // Check if invoice has any of the specified statuses in InvoiceApprovalHistories
                SubmittedInvoices = g.Count(i => i.InvoiceApprovalHistory
                    .Any(h => h.InvoiceStatus == Domain.Enums.InvoiceStatus.SIGNED)),
                CompletedInvoices = g.Count(i => i.InvoiceApprovalHistory
                    .Any(h => h.InvoiceStatus == Domain.Enums.InvoiceStatus.TRANSMITTED)),
                RejectedInvoices = g.Count(i => i.InvoiceStatus == Domain.Enums.InvoiceStatus.REJECTED),
                PaidInvoices = g.Count(i => i.PaymentStatus == PaymentStatus.Paid),
                TotalAmount = g.SelectMany(i => i.InvoiceLine)
                               .Sum(il => (decimal)(il.Quantity * il.BusinessItem.UnitPrice))
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Handle case where no invoices exist in date range
        if (stats == null)
        {
            // Return empty analytics with zero values
            return new DashboardAnalyticsDto(
                TotalInvoices: 0,
                DraftInvoices: 0,
                SubmittedInvoices: 0,
                ApprovedInvoices: 0,
                RejectedInvoices: 0,
                PaidInvoices: 0,
                TotalInvoiceAmount: 0.00M,
                AverageProcessingTimeDays: 0.0,
                SuccessRatePercentage: 0.0M,
                RejectionRatePercentage: 0.0M,
                MonthlySales: new List<MonthlySalesDataDto>(),
                MonthlyPurchases: new List<MonthlyPurchasesDataDto>(),
                MonthlyComparison: new MonthlyComparisonDto(0, 0, 0, 0, "")
            );
        }

        // Calculate total sales amount from invoices (outgoing invoices)
        var invoicesWithItems = await query
            .AsNoTracking()
            .Select(i => new
            {
                InvoiceId = i.Id,
                Items = i.InvoiceLine.Select(line => new
                {
                    Quantity = line.Quantity,
                    UnitPrice = line.BusinessItem != null ? line.BusinessItem.UnitPrice : 0.0m,
                    DiscountFee = line.DiscountFee != null ? Convert.ToDecimal(line.DiscountFee.Amount) : 0.0m,
                    AdditionalFee = line.AdditionalFee != null ? Convert.ToDecimal(line.AdditionalFee.Amount) : 0.0m
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var invoice in invoicesWithItems)
        {
            foreach (var line in invoice.Items)
            {
                decimal lineTotal = Convert.ToDecimal(line.Quantity * line.UnitPrice);
                lineTotal -= line.DiscountFee;
                lineTotal += line.AdditionalFee;
                totalSalesAmount += lineTotal;
            }
        }

        // Calculate total purchases amount from ReceivedInvoices table
        var receivedInvoicesQuery = _context.ReceivedInvoices.AsNoTracking().AsQueryable();

        // Apply security filters - Business admins can only see their own business statistics
        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            receivedInvoicesQuery = receivedInvoicesQuery.Where(r => r.BusinessId == _currentUserService.BusinessId.Value);

        totalPurchasesAmount = await receivedInvoicesQuery
            .Where(r => r.CreatedAt.Date >= startDate.Date && r.CreatedAt.Date < endDate.AddDays(1).Date)
            .SumAsync(r => r.PayableAmount, cancellationToken);

        // Calculate Collection Performance Metrics
        double averageProcessingTimeDays = 0.0;
        decimal successRatePercentage = 0.0M;
        decimal rejectionRatePercentage = 0.0M;

        // Get invoices with processing time data (submitted invoices that have been processed)
        var processedInvoices = await query
            .Where(i => i.FIRSSubmissionResponseMessage== "Invoice transmitted successfully" &&
                       (i.InvoiceStatus == Domain.Enums.InvoiceStatus.TRANSMITTED 
                      ))
            .Select(i => new
            {
                i.CreatedAt,
                i.UpdatedAt,
                i.InvoiceStatus
            })
            .ToListAsync(cancellationToken);

        // Calculate average processing time (from creation to FIRS submission)
        if (processedInvoices.Any())
        {
            var processingTimes = processedInvoices
                .Where(i => i.UpdatedAt.HasValue)
                .Select(i => (i.UpdatedAt!.Value - i.CreatedAt).TotalDays)
                .ToList();

            if (processingTimes.Any())
            {
                averageProcessingTimeDays = processingTimes.Average();
            }
        }

        // Calculate success rate (approved + acknowledged invoices / total processed invoices)
        var totalProcessedInvoices = processedInvoices.Count;
        if (totalProcessedInvoices > 0)
        {
            var successfulInvoices = processedInvoices
                .Count(i => i.InvoiceStatus == Domain.Enums.InvoiceStatus.APPROVED ||
                           i.InvoiceStatus == Domain.Enums.InvoiceStatus.VALIDATED || i.InvoiceStatus == Domain.Enums.InvoiceStatus.SIGNED || i.InvoiceStatus == Domain.Enums.InvoiceStatus.TRANSMITTED);

            successRatePercentage = Math.Round((decimal)successfulInvoices / totalProcessedInvoices * 100, 2);
        }

        // Calculate rejection rate (rejected invoices / total processed invoices)
        if (totalProcessedInvoices > 0)
        {
            var rejectedCount = processedInvoices
                .Count(i => i.InvoiceStatus == Domain.Enums.InvoiceStatus.REJECTED);

            rejectionRatePercentage = Math.Round((decimal)rejectedCount / totalProcessedInvoices * 100, 2);
        }

        // Get monthly sales data for the last 12 months
        // 1. Get raw data from database
        var monthlySalesRawData = await query
            .Where(i => i.CreatedAt.Date >= monthlyStartDate.Date && i.CreatedAt.Date <= monthlyEndDate.Date)
            .AsNoTracking()
            .Select(i => new
            {
                Year = i.CreatedAt.Year,
                Month = i.CreatedAt.Month,
                InvoiceLines = i.InvoiceLine.Select(il => new
                {
                    Quantity = il.Quantity,
                    UnitPrice = il.BusinessItem != null ? il.BusinessItem.UnitPrice : 0.0m,
                    DiscountFee = il.DiscountFee,
                    AdditionalFee = il.AdditionalFee
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        // 2. Transform to DTO with calculations
        var monthlySalesData = monthlySalesRawData
            .GroupBy(i => new { i.Year, i.Month })
            .Select(g => new MonthlySalesDataDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                SalesAmount = g.SelectMany(i => i.InvoiceLines)
                    .Sum(il => Convert.ToDecimal(il.Quantity * il.UnitPrice)
                        - (il.DiscountFee != null ? Convert.ToDecimal(il.DiscountFee.Amount) : 0.0m)
                        + (il.AdditionalFee != null ? Convert.ToDecimal(il.AdditionalFee.Amount) : 0.0m))
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        // 3. Fill missing months with zeros
        var allMonthlyData = Enumerable.Range(0, 12)
            .Select(i => new
            {
                Date = monthlyStartDate.AddMonths(i),
                Year = monthlyStartDate.AddMonths(i).Year,
                Month = monthlyStartDate.AddMonths(i).Month,
                MonthName = monthlyStartDate.AddMonths(i).ToString("MMM")
            })
            .Select(d => new MonthlySalesDataDto
            {
                Year = d.Year,
                Month = d.Month,
                MonthName = d.MonthName,
                SalesAmount = monthlySalesData  // Now this exists
                    .FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month)?.SalesAmount ?? 0
            })
            .ToList();

        // Do the same for purchases
        var monthlyPurchasesRawData = await receivedInvoicesQuery
            .Where(r => r.CreatedAt.Date >= monthlyStartDate.Date && r.CreatedAt.Date <= monthlyEndDate.Date)
            .AsNoTracking()
            .Select(r => new
            {
                Year = r.CreatedAt.Year,
                Month = r.CreatedAt.Month,
                PayableAmount = r.PayableAmount
            })
            .ToListAsync(cancellationToken);

        var monthlyPurchasesData = monthlyPurchasesRawData
            .GroupBy(r => new { r.Year, r.Month })
            .Select(g => new MonthlyPurchasesDataDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                PurchasesAmount = g.Sum(r => r.PayableAmount)
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        var allMonthlyPurchasesData = Enumerable.Range(0, 12)
            .Select(i => new
            {
                Date = monthlyStartDate.AddMonths(i),
                Year = monthlyStartDate.AddMonths(i).Year,
                Month = monthlyStartDate.AddMonths(i).Month,
                MonthName = monthlyStartDate.AddMonths(i).ToString("MMM")
            })
            .Select(d => new MonthlyPurchasesDataDto
            {
                Year = d.Year,
                Month = d.Month,
                MonthName = d.MonthName,
                PurchasesAmount = monthlyPurchasesData  // Now this exists
                    .FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month)?.PurchasesAmount ?? 0
            })
            .ToList();

        // Calculate monthly comparison (current month vs previous month)
        var currentDate = DateTime.Now;
        var currentMonth = currentDate.Month;
        var currentYear = currentDate.Year;
        var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        var currentMonthSales = allMonthlyData
     .FirstOrDefault(m => m.Year == currentYear && m.Month == currentMonth)?.SalesAmount ?? 0;

        var previousMonthSales = allMonthlyData
            .FirstOrDefault(m => m.Year == previousYear && m.Month == previousMonth)?.SalesAmount ?? 0;

        var currentMonthPurchases = allMonthlyPurchasesData
            .FirstOrDefault(m => m.Year == currentYear && m.Month == currentMonth)?.PurchasesAmount ?? 0;

        var previousMonthPurchases = allMonthlyPurchasesData
            .FirstOrDefault(m => m.Year == previousYear && m.Month == previousMonth)?.PurchasesAmount ?? 0;

        // Calculate percentage changes
        decimal salesPercentageChange;
        decimal purchasesPercentageChange;
        string salesComparisonString;

        if (previousMonthSales > 0)
        {
            salesPercentageChange = Math.Round(((currentMonthSales - previousMonthSales) / previousMonthSales) * 100, 1);
            salesComparisonString = salesPercentageChange >= 0
                ? $"+{salesPercentageChange:F1}%"
                : $"{salesPercentageChange:F1}%";
        }
        else if (currentMonthSales > 0)
        {
            salesPercentageChange = 100;
            salesComparisonString = "+100%";
        }
        else
        {
            salesPercentageChange = 0;
            salesComparisonString = "0.0%";
        }

        if (previousMonthPurchases > 0)
        {
            purchasesPercentageChange = Math.Round(((currentMonthPurchases - previousMonthPurchases) / previousMonthPurchases) * 100, 1);
        }
        else if (currentMonthPurchases > 0)
        {
            purchasesPercentageChange = 100;
        }
        else
        {
            purchasesPercentageChange = 0;
        }

        var monthlyComparison = new MonthlyComparisonDto
        {
            CurrentMonthSales = currentMonthSales,
            CurrentMonthPurchases = currentMonthPurchases,
            SalesPercentageChange = salesPercentageChange,
            PurchasesPercentageChange = purchasesPercentageChange,
            SalesComparisonString = salesComparisonString
        };

        return new DashboardAnalyticsDto(
            stats.TotalInvoices,
            stats.DraftInvoices,
            stats.SubmittedInvoices,
            stats.CompletedInvoices,
            stats.RejectedInvoices,
            stats.PaidInvoices,
            stats.TotalAmount,
            averageProcessingTimeDays,
            successRatePercentage,
            rejectionRatePercentage,
            allMonthlyData,
            allMonthlyPurchasesData,
            monthlyComparison
        );
    }

    private static (DateTime startDate, DateTime endDate) GetWeekRange(DateTime date)
    {
        var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        return (startOfWeek, endOfWeek);
    }

    private static (DateTime startDate, DateTime endDate) GetMonthRange(DateTime date)
    {
        var startOfMonth = new DateTime(date.Year, date.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        return (startOfMonth, endOfMonth);
    }

    
}
