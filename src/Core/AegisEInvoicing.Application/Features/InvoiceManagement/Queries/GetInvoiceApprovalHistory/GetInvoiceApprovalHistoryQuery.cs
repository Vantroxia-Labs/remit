using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceApprovalHistory;

public record GetInvoiceApprovalHistoryQuery(int PageNumber, int PageSize) : IRequest<PaginatedList<InvoiceApprovalHistoryDto>>;
