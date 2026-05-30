using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastList;

public record GetBroadcastListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    BroadcastStatus? Status = null) : IRequest<PaginatedList<InvoiceBroadcastSummaryDto>>;
