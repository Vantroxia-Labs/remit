using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Queries.GetBusinessItemList;

public record GetBusinessItemListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PaginatedList<BusinessItemSummaryDto>>;