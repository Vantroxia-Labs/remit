using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorGroupList;

public record GetVendorGroupListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null) : IRequest<PaginatedList<VendorGroupSummaryDto>>;
