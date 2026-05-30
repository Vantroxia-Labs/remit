using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetPendingApprovalInvoices;

public class GetPendingApprovalInvoicesQueryHandler : IRequestHandler<GetPendingApprovalInvoicesQuery, PaginatedList<InvoiceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetPendingApprovalInvoicesQueryHandler> _logger;

    public GetPendingApprovalInvoicesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetPendingApprovalInvoicesQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PaginatedList<InvoiceDto>> Handle(GetPendingApprovalInvoicesQuery request, CancellationToken cancellationToken)
    {
        // Authorization is enforced by [RequireRole] attribute at controller level
        // This check is a safety net - return empty if somehow bypassed
        if (!_currentUserService.HasRole(RoleConstants.ClientAdmin) || !_currentUserService.BusinessId.HasValue)
        {
            _logger.LogWarning(
                "Unauthorized access attempt to pending approval invoices. UserId: {UserId}, HasClientAdmin: {HasRole}, BusinessId: {BusinessId}",
                _currentUserService.UserId,
                _currentUserService.HasRole(RoleConstants.ClientAdmin),
                _currentUserService.BusinessId);
            return new PaginatedList<InvoiceDto>([], 0, request.PageNumber, request.PageSize);
        }

        // Note: InvoiceLine is not included as it's not used in the projection
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
            .Include(i => i.Party)
            .Include(i => i.Business)
            .Include(i => i.InvoiceApprovalHistory)
            .Where(i => i.BusinessId == _currentUserService.BusinessId.Value)
            .Where(i => i.InvoiceStatus == InvoiceStatus.PENDING_APPROVAL)
            .Where(i => !i.IsDeleted)
            .AsQueryable();

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

        query = ApplyOrdering(query, request.OrderBy, request.OrderByDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(invoice => new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                BusinessId = invoice.BusinessId,
                BusinessName = invoice.Business.Name,
                PartyName = invoice.Party != null ? invoice.Party.Name : null,
                InvoiceStatus = invoice.InvoiceApprovalHistory
                    .Select(x => x.InvoiceStatus)
                    .OrderBy(x => x)
                    .ToArray(),
                Irn = invoice.Irn != null ? invoice.Irn.Value : string.Empty,
                Status = invoice.InvoiceStatus,
                FirsResponseMessage = "Pending ClientAdmin Approval",
                InvoiceSource = invoice.InvoiceSource,
                PaymentStatus = invoice.PaymentStatus,
                IssueDate = invoice.IssueDate,
                TotalAmount = invoice.InvoiceLine.Sum(il => (decimal)(il.Quantity * (il.BusinessItem != null ? il.BusinessItem.UnitPrice : 0.0m))),
                CreatedAt = invoice.CreatedAt,
                CreatedBy = invoice.CreatedBy.ToString()
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} pending approval invoices for business {BusinessId}",
            items.Count, _currentUserService.BusinessId);

        return new PaginatedList<InvoiceDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IQueryable<Invoice> ApplyOrdering(IQueryable<Invoice> query, string? orderBy, bool descending)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return descending
                ? query.OrderByDescending(i => i.CreatedAt)
                : query.OrderBy(i => i.CreatedAt);
        }

        Expression<Func<Invoice, object>> orderExpression = orderBy.ToLower() switch
        {
            "irn" => i => i.Irn.Value,
            "issuedate" => i => i.IssueDate,
            "createdat" => i => i.CreatedAt,
            "status" => i => i.InvoiceStatus,
            _ => i => i.CreatedAt
        };

        return descending
            ? query.OrderByDescending(orderExpression)
            : query.OrderBy(orderExpression);
    }
}
