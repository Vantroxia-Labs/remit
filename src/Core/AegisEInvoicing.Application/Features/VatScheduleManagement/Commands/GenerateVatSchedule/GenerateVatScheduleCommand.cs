using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Commands.GenerateVatSchedule;

/// <summary>
/// Generates a new VAT schedule for a given year/month.
/// Captures all transmitted invoices for that period that have not yet been
/// assigned to any existing schedule.
/// </summary>
public record GenerateVatScheduleCommand : IRequest<VatScheduleDto>, ITransactionalCommand
{
    public int Year { get; init; }
    public int Month { get; init; }
}
