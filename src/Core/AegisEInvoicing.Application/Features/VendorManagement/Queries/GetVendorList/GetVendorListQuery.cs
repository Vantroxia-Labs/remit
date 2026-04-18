using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorList;

public record GetVendorListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? VendorGroupId = null,
    string? SearchTerm = null) : IRequest<PaginatedList<VendorSummaryDto>>;
