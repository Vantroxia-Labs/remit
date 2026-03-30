using AegisEInvoicing.Application.Features.DashboardAnalytics.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.DashboardAnalytics.Queries;

public record GetDashboardAnalyticsQuery(bool ThisWeek) : IRequest<DashboardAnalyticsDto>;
