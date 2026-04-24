using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Queries.GetWhtSchedules;

/// <summary>Returns all WHT schedules for the current user's business, optionally filtered by year and environment mode.</summary>
public record GetWhtSchedulesQuery(int? Year = null, AppEnvironmentMode? EnvironmentMode = null) : IRequest<List<WhtScheduleDto>>;
