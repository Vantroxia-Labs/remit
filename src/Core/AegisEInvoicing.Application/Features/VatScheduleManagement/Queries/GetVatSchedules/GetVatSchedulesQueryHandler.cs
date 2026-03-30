using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Queries.GetVatSchedules;

public class GetVatSchedulesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetVatSchedulesQueryHandler> logger)
    : IRequestHandler<GetVatSchedulesQuery, List<VatScheduleDto>>
{
    public async Task<List<VatScheduleDto>> Handle(GetVatSchedulesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.BusinessId.HasValue)
        {
            logger.LogWarning("GetVatSchedules called with no BusinessId for user {UserId}", currentUser.UserId);
            return [];
        }

        var query = context.VatSchedules
            .AsNoTracking()
            .Where(s => s.BusinessId == currentUser.BusinessId.Value);

        if (request.Year.HasValue)
            query = query.Where(s => s.Year == request.Year.Value);

        var schedules = await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .Select(s => new VatScheduleDto
            {
                Id = s.Id,
                Year = s.Year,
                Month = s.Month,
                MonthName = s.MonthName,
                PeriodStart = s.PeriodStart,
                PeriodEnd = s.PeriodEnd,
                DueDate = s.DueDate,
                Status = s.Status.ToString(),
                FiledAt = s.FiledAt,
                GeneratedAt = s.CreatedAt,
                TotalInvoiceCount = s.TotalInvoiceCount,
                TotalTaxableAmount = s.TotalTaxableAmount,
                TotalVatAmount = s.TotalVatAmount,
                Items = new List<VatScheduleItemDto>(), // items loaded separately via GetVatScheduleWithItems
            })
            .ToListAsync(cancellationToken);

        return schedules;
    }
}
