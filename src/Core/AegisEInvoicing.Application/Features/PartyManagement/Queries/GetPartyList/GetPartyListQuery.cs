using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartyList;

public record GetPartyListQuery(
    Guid? BusinessId,
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = "Name",
    bool SortDescending = false) : IRequest<PaginatedList<PartySummaryDto>>;