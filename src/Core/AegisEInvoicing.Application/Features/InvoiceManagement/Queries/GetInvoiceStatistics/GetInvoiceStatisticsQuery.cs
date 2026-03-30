using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceStatistics;

public record GetInvoiceStatisticsQuery(
    Guid? BusinessId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null) : IRequest<InvoiceStatisticsDto>;