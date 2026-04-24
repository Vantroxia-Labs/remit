using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Queries.GetVatSchedules;

/// <summary>Returns all VAT schedules for the current user's business, optionally filtered by year and environment mode.</summary>
public record GetVatSchedulesQuery(int? Year = null, AppEnvironmentMode? EnvironmentMode = null) : IRequest<List<VatScheduleDto>>;
