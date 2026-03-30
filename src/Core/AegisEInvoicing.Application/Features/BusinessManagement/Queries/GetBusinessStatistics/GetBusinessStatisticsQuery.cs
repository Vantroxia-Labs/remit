using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinessStatistics;

public record GetBusinessStatisticsQuery(
    Guid? BusinessId = null) : IRequest<BusinessStatisticsDto>;