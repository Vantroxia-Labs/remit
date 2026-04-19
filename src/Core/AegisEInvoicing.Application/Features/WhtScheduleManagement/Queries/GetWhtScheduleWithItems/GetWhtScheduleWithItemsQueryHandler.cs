using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Queries.GetWhtScheduleWithItems;

public class GetWhtScheduleWithItemsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<GetWhtScheduleWithItemsQuery, WhtScheduleDto>
{
    public async Task<WhtScheduleDto> Handle(GetWhtScheduleWithItemsQuery request, CancellationToken cancellationToken)
    {
        var schedule = await context.WhtSchedules
            .AsNoTracking()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

        if (schedule is null)
            throw new NotFoundException($"WHT schedule {request.ScheduleId} not found.");

        if (!currentUser.IsPlatformAdmin && schedule.BusinessId != currentUser.BusinessId)
            throw new ForbiddenException("Access denied.");

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
            Items = schedule.Items.Select(i => new WhtScheduleItemDto
            {
                Id = i.Id,
                ReceivedInvoiceId = i.ReceivedInvoiceId,
                VendorName = i.VendorName,
                VendorAddress = i.VendorAddress,
                VendorTin = i.VendorTin,
                Irn = i.Irn,
                IssueDate = i.IssueDate,
                NatureOfTransaction = i.NatureOfTransaction.ToString(),
                GrossAmount = i.GrossAmount,
                WhtRate = i.WhtRate,
                WhtAmount = i.WhtAmount,
                NetAmount = i.NetAmount,
                TaxAuthority = i.TaxAuthority.ToString(),
            }).ToList(),
        };
    }
}
