using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Commands.MarkWhtScheduleFiled;

public class MarkWhtScheduleFiledCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<MarkWhtScheduleFiledCommandHandler> logger)
    : IRequestHandler<MarkWhtScheduleFiledCommand, WhtScheduleDto>
{
    public async Task<WhtScheduleDto> Handle(MarkWhtScheduleFiledCommand request, CancellationToken cancellationToken)
    {
        var schedule = await context.WhtSchedules
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

        if (schedule is null)
            throw new NotFoundException($"WHT schedule {request.ScheduleId} not found.");

        if (!currentUser.IsPlatformAdmin && schedule.BusinessId != currentUser.BusinessId)
            throw new ForbiddenException("Access denied.");

        schedule.MarkAsFiled();

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WHT schedule {ScheduleId} ({Year}-{Month}) marked as filed by user {UserId}.",
            schedule.Id, schedule.Year, schedule.Month, currentUser.UserId);

        return new WhtScheduleDto
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
        };
    }
}
