using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Linq.Expressions;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetAllInvoices;

public class GetAllInvoicesQueryHandler : IRequestHandler<GetAllInvoicesQuery, PaginatedList<InvoiceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetAllInvoicesQueryHandler> _logger;

    public GetAllInvoicesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetAllInvoicesQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PaginatedList<InvoiceDto>> Handle(GetAllInvoicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceLine)
                .Include(i => i.Business)
                .Include(i => i.InvoiceApprovalHistory)
                .Include(i => i.CreatedByUser)
                .AsQueryable();

            if (!_currentUserService.IsPlatformAdmin)
            {
                if (_currentUserService.BusinessId.HasValue)
                {
                    query = query.Where(i => i.BusinessId == _currentUserService.BusinessId.Value);
                }
                else
                {
                    _logger.LogWarning("Non-admin user without business ID attempting to retrieve invoices");
                    return new PaginatedList<InvoiceDto>(new List<InvoiceDto>(), 0, request.PageNumber, request.PageSize);
                }
            }
            else if (request.BusinessId.HasValue)
            {
                query = query.Where(i => i.BusinessId == request.BusinessId.Value);
            }

            if (request.InvoiceStatus.HasValue)
            {
                query = query.Where(i => i.InvoiceStatus == request.InvoiceStatus.Value);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(i => i.IssueDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(i => i.IssueDate <= request.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(i =>
                    i.Irn.Value.ToLower().Contains(searchTerm) ||
                    (i.Note != null && i.Note.ToLower().Contains(searchTerm)));
            }

            if (request.EnvironmentMode.HasValue)
            {
                query = query.Where(i => i.EnvironmentMode == request.EnvironmentMode.Value);
            }

            // Exclude deleted invoices
            query = query.Where(inv => !inv.IsDeleted);

            // Exclude invoices pending approval (use dedicated endpoint for those)
            query = query.Where(inv => inv.InvoiceStatus != InvoiceStatus.PENDING_APPROVAL);

            query = ApplyOrdering(query, request.OrderBy, request.OrderByDescending);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(invoice => new InvoiceDto
                {
                    Id = invoice.Id,
                    BusinessId = invoice.BusinessId,
                    BusinessName = invoice.Business.Name,
                    InvoiceStatus = invoice.InvoiceApprovalHistory.Select(x => x.InvoiceStatus).OrderBy(x => x).ToArray(),
                    Irn = invoice.Irn.Value,
                    CurrentInvoiceStatus = invoice.InvoiceStatus,
                    FirsResponseMessage = FirsResponse(invoice.FIRSSubmissionResponseMessage, invoice.InvoiceStatus),
                    InvoiceSource = invoice.InvoiceSource,
                    PaymentStatus = invoice.PaymentStatus,
                    IssueDate = invoice.IssueDate,
                    CreatedAt = invoice.CreatedAt,
                    CreatedBy = invoice.CreatedByUser.ToString()
                })
                .ToListAsync(cancellationToken);

            return new PaginatedList<InvoiceDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            throw;
        }
    }

    private static IQueryable<Invoice> ApplyOrdering(IQueryable<Invoice> query, string? orderBy, bool descending)
    {
        Expression<Func<Invoice, object>> keySelector = orderBy?.ToLower() switch
        {
            "irn" => i => i.Irn,
            "issuedate" => i => i.IssueDate,
            "duedate" => i => i.DueDate ?? DateOnly.MaxValue,
            "status" => i => i.InvoiceStatus,
            "paymentstatus" => i => i.PaymentStatus,
            "createdat" => i => i.CreatedAt,
            _ => i => i.CreatedAt
        };

        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    private static string FirsResponse(string? message, InvoiceStatus invoiceStatus)
    {
        return invoiceStatus switch
        {
            InvoiceStatus.REJECTED => message ?? "Invoice Rejected",
            InvoiceStatus.VALIDATIONFAILED => message ?? "Invoice FIRS Validation Failed",
            InvoiceStatus.SIGNINGFAILED => message ?? "Invoice FIRS SIGNING Failed",
            InvoiceStatus.FAILED => message ?? "Invoice Transmission Failed",
            InvoiceStatus.CREATED => message ?? "Invoice Created",
            InvoiceStatus.APPROVED => message ?? "Invoice Approved",
            InvoiceStatus.VALIDATED => message ?? "Invoice FIRS Validation Successful",
            InvoiceStatus.SIGNED => message ?? "Invoice FIRS Signing Successful",
            _ => "Invoice Process Completed"
        };
    }
}