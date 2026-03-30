using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceStatistics;

public class GetInvoiceStatisticsQueryHandler : IRequestHandler<GetInvoiceStatisticsQuery, InvoiceStatisticsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetInvoiceStatisticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InvoiceStatisticsDto> Handle(GetInvoiceStatisticsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Invoices.AsQueryable();

        // Apply security filters
        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
        {
            query = query.Where(i => i.BusinessId == _currentUserService.BusinessId!.Value);
        }

        if (request.BusinessId.HasValue)
        {
            query = query.Where(i => i.BusinessId == request.BusinessId.Value);
        }

        // Apply date filters
        var fromDate = request.FromDate ?? DateTimeOffset.UtcNow.AddYears(-1);
        var toDate = request.ToDate ?? DateTimeOffset.UtcNow;

        var filteredQuery = query.Where(i => i.CreatedAt >= fromDate && i.CreatedAt <= toDate);

        // Calculate statistics
        var totalInvoices = await filteredQuery.CountAsync(cancellationToken);
        var draftInvoices = await filteredQuery.CountAsync(i => i.InvoiceStatus == InvoiceStatus.DRAFT, cancellationToken);
        var submittedInvoices = await filteredQuery.CountAsync(i => i.InvoiceStatus == InvoiceStatus.SUBMITTED, cancellationToken);
        var approvedInvoices = await filteredQuery.CountAsync(i => i.InvoiceStatus == InvoiceStatus.APPROVED, cancellationToken);
        var rejectedInvoices = await filteredQuery.CountAsync(i => i.InvoiceStatus == InvoiceStatus.REJECTED, cancellationToken);

        

        // Monthly statistics
        var currentMonth = DateTimeOffset.UtcNow.Date.AddDays(1 - DateTimeOffset.UtcNow.Day);
        var monthlyQuery = query.Where(i => i.CreatedAt >= currentMonth);
        var invoicesThisMonth = await monthlyQuery.CountAsync(cancellationToken);

        // Weekly statistics
        var currentWeekStart = DateTimeOffset.UtcNow.Date.AddDays(-(int)DateTimeOffset.UtcNow.DayOfWeek);
        var invoicesThisWeek = await query
            .CountAsync(i => i.CreatedAt >= currentWeekStart, cancellationToken);

        return new InvoiceStatisticsDto(
            totalInvoices,
            draftInvoices,
            submittedInvoices,
            approvedInvoices,
            rejectedInvoices,
            invoicesThisMonth,
            invoicesThisWeek);
    }
}