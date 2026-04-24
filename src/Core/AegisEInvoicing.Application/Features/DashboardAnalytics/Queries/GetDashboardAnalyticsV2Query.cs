using AegisEInvoicing.Application.Features.DashboardAnalytics.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.DashboardAnalytics.Queries;

public record GetDashboardAnalyticsV2Query(DashboardType DashboardType, AppEnvironmentMode? EnvironmentMode = null) : IRequest<DashboardAnalyticsV2Dto>;
