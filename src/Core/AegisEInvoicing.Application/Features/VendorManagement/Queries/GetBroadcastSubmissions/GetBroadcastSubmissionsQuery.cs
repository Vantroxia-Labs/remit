using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastSubmissions;

public record GetBroadcastSubmissionsQuery(
    Guid BroadcastId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<BroadcastVendorSubmissionDto>>;
