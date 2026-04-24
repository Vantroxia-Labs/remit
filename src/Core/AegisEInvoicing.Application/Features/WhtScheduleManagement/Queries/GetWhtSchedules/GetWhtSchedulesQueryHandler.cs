using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Queries.GetWhtSchedules;

public class GetWhtSchedulesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetWhtSchedulesQueryHandler> logger)
    : IRequestHandler<GetWhtSchedulesQuery, List<WhtScheduleDto>>
{
    public async Task<List<WhtScheduleDto>> Handle(GetWhtSchedulesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.BusinessId.HasValue)
        {
            logger.LogWarning("GetWhtSchedules called with no BusinessId for user {UserId}", currentUser.UserId);
            return [];
        }

        var query = context.WhtSchedules
            .AsNoTracking()
            .Where(s => s.BusinessId == currentUser.BusinessId.Value);

        // TODO: Filter by EnvironmentMode once property is added to WhtSchedule entity
        // if (request.EnvironmentMode.HasValue)
        //     query = query.Where(s => s.EnvironmentMode == request.EnvironmentMode.Value);

        if (request.Year.HasValue)
            query = query.Where(s => s.Year == request.Year.Value);

        var schedules = await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .Select(s => new WhtScheduleDto
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
                TotalItemCount = s.TotalItemCount,
                TotalGrossAmount = s.TotalGrossAmount,
                TotalWhtAmount = s.TotalWhtAmount,
                TotalNrsWhtAmount = s.TotalNrsWhtAmount,
                TotalStateWhtAmount = s.TotalStateWhtAmount,
            })
            .ToListAsync(cancellationToken);

        return schedules;
    }
}
