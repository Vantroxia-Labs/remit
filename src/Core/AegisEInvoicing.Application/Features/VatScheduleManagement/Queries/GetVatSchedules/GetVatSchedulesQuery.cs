using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Queries.GetVatSchedules;

/// <summary>Returns all VAT schedules for the current user's business, optionally filtered by year.</summary>
public record GetVatSchedulesQuery(int? Year = null) : IRequest<List<VatScheduleDto>>;
