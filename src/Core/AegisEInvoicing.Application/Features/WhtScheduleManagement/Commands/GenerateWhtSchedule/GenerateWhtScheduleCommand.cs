using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Commands.GenerateWhtSchedule;

/// <summary>
/// Generates a new WHT schedule for a given year/month.
/// Captures all received invoices (B2B and B2G) that have not yet been
/// assigned to any existing WHT schedule.
/// </summary>
public record GenerateWhtScheduleCommand : IRequest<WhtScheduleDto>, ITransactionalCommand
{
    public int Year { get; init; }
    public int Month { get; init; }
}
