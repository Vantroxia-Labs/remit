using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinesses;

public record GetBusinessesQuery(
    Guid? BusinessId = null, 
    string? SearchTerm = null, 
    BusinessStatus? Status = null,
    bool? IsConnectedToFIRS = null, 
    int PageNumber = 1, 
    int PageSize = 20) : IRequest<PaginatedList<BusinessUsersSummaryDto>>;