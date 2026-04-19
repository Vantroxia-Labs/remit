using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Queries.GetWhtScheduleWithItems;

/// <summary>Returns a single WHT schedule including all line-item snapshots.</summary>
public record GetWhtScheduleWithItemsQuery(Guid ScheduleId) : IRequest<WhtScheduleDto>;
