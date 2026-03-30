using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Queries.GetVatScheduleWithItems;

/// <summary>Returns a single VAT schedule including all line-item snapshots.</summary>
public record GetVatScheduleWithItemsQuery(Guid ScheduleId) : IRequest<VatScheduleDto>;
