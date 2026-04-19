using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Commands.MarkVatScheduleFiled;

public class MarkVatScheduleFiledCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<MarkVatScheduleFiledCommandHandler> logger)
    : IRequestHandler<MarkVatScheduleFiledCommand, VatScheduleDto>
{
    public async Task<VatScheduleDto> Handle(MarkVatScheduleFiledCommand request, CancellationToken cancellationToken)
    {
        var schedule = await context.VatSchedules
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

        if (schedule is null)
            throw new NotFoundException($"VAT schedule {request.ScheduleId} not found.");

        if (!currentUser.IsPlatformAdmin && schedule.BusinessId != currentUser.BusinessId)
            throw new ForbiddenException("Access denied.");

        schedule.MarkAsFiled();

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "VAT schedule {ScheduleId} ({Year}-{Month}) marked as filed by user {UserId}.",
            schedule.Id, schedule.Year, schedule.Month, currentUser.UserId);

        return new VatScheduleDto
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
            TotalInvoiceCount = schedule.TotalInvoiceCount,
            TotalTaxableAmount = schedule.TotalTaxableAmount,
            TotalVatAmount = schedule.TotalVatAmount,
            TotalInputInvoiceCount = schedule.TotalInputInvoiceCount,
            TotalInputTaxableAmount = schedule.TotalInputTaxableAmount,
            TotalInputVatAmount = schedule.TotalInputVatAmount,
            NetVatPayable = schedule.NetVatPayable,
        };
    }
}
