using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Queries.GetVatScheduleWithItems;

public class GetVatScheduleWithItemsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<GetVatScheduleWithItemsQuery, VatScheduleDto>
{
    private readonly ILogger<GetVatScheduleWithItemsQueryHandler>? _logger;

    public GetVatScheduleWithItemsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetVatScheduleWithItemsQueryHandler> logger) : this(context, currentUser)
    {
        _logger = logger;
    }
    public async Task<VatScheduleDto> Handle(GetVatScheduleWithItemsQuery request, CancellationToken cancellationToken)
    {
        var schedule = await context.VatSchedules
            .AsNoTracking()
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId, cancellationToken);

        if (schedule is null)
            throw new NotFoundException($"VAT schedule {request.ScheduleId} not found.");

        // Non-platform admins can only read their own business schedules
        if (!currentUser.IsPlatformAdmin && schedule.BusinessId != currentUser.BusinessId)
            throw new ForbiddenException("Access denied.");

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
            Items = schedule.Items.Select(i => new VatScheduleItemDto
            {
                Id = i.Id,
                InvoiceId = i.InvoiceId,
                InvoiceCode = i.InvoiceCode,
                Irn = i.Irn,
                PartyName = i.PartyName,
                PartyTin = i.PartyTin,
                IssueDate = i.IssueDate,
                TaxableAmount = i.TaxableAmount,
                VatAmount = i.VatAmount,
                TotalAmount = i.TotalAmount,
                PaymentStatus = i.PaymentStatus.ToString(),
            }).ToList(),
        };
    }
}
