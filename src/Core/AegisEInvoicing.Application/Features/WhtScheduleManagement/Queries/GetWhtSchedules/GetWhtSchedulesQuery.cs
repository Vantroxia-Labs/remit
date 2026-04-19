using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Queries.GetWhtSchedules;

/// <summary>Returns all WHT schedules for the current user's business, optionally filtered by year.</summary>
public record GetWhtSchedulesQuery(int? Year = null) : IRequest<List<WhtScheduleDto>>;
