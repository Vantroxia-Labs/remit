using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetAllInvoicesForBusiness;

public class GetAllInvoicesForBusinessQueryHandler : IRequestHandler<GetAllInvoicesForBusinessQuery, GetAllInvoicesForBusinessResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetAllInvoicesForBusinessQueryHandler> _logger;

    public GetAllInvoicesForBusinessQueryHandler(
        IApplicationDbContext context, 
        ICurrentUserService currentUserService,
        ILogger<GetAllInvoicesForBusinessQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetAllInvoicesForBusinessResult> Handle(GetAllInvoicesForBusinessQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user has business access
            if (request.BusinessId==Guid.Empty)
            {
                return new GetAllInvoicesForBusinessResult
                {
                    Success = false,
                    Message = "User not authenticated or no business associated"
                };
            }

            var businessId = request.BusinessId;
            
            _logger.LogInformation("Retrieving invoices for business: {BusinessId}, Page: {PageNumber}, Size: {PageSize}",
                businessId, request.PageNumber, request.PageSize);

            // Start with base query filtering by business
            var query = _context.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceLine)
                    .ThenInclude(il => il.BusinessItem)
                .Include(i => i.Party)
                .Include(i => i.Business)
                .Include(i => i.InvoiceApprovalHistory)
                .Where(i => i.BusinessId == businessId);

            // Apply filters
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
                // Sanitize search term to prevent injection attacks (VAPT finding: time-based SQL injection)
                var searchTerm = InputSanitizationService.SanitizeSearchTerm(request.SearchTerm);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(i =>
                        i.Irn.Value.ToLower().Contains(searchTerm) ||
                        i.InvoiceCode.ToLower().Contains(searchTerm) ||
                        (i.Note != null && i.Note.ToLower().Contains(searchTerm)) ||
                        (i.FIRSSubmissionId != null && i.FIRSSubmissionId.ToLower().Contains(searchTerm))
                    );
                }
            }

            // Apply ordering
            if (!string.IsNullOrWhiteSpace(request.OrderBy))
            {
                query = ApplyOrdering(query, request.OrderBy, request.OrderByDescending);
            }
            else
            {
                // Default ordering
                query = request.OrderByDescending 
                    ? query.OrderByDescending(i => i.CreatedAt)
                    : query.OrderBy(i => i.CreatedAt);
            }

            query = query.Where(inv => !inv.IsDeleted);

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var invoices = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(i => new InvoiceDto
                {
                    Id = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    BusinessId = i.BusinessId,
                    BusinessName = i.Business.Name,
                    PartyName = i.Party != null ? i.Party.Name : null,
                    Irn = i.Irn != null ? i.Irn.Value : string.Empty,
                    InvoiceSource = i.InvoiceSource,
                    Status = i.InvoiceStatus,
                    FirsResponseMessage = FirsResponse(i.FIRSSubmissionResponseMessage, i.InvoiceStatus),
                    InvoiceStatus = i.InvoiceApprovalHistory.Select(x => x.InvoiceStatus).OrderBy(x => x).ToArray(),
                    PaymentStatus = i.PaymentStatus,
                    IssueDate = i.IssueDate,
                    TotalAmount = i.InvoiceLine.Sum(il => (decimal)(il.Quantity * (il.BusinessItem != null ? il.BusinessItem.UnitPrice : 0.0m))),
                    CreatedAt = i.CreatedAt,
                    CreatedBy = i.CreatedBy.ToString()
                })
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedList<InvoiceDto>(
                invoices, totalCount, request.PageNumber, request.PageSize);

            _logger.LogInformation("Successfully retrieved {Count} invoices for business: {BusinessId}",
                invoices.Count, businessId);

            return new GetAllInvoicesForBusinessResult
            {
                Success = true,
                Message = $"Retrieved {invoices.Count} invoices",
                Invoices = paginatedResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for business: {BusinessId}",
                request.BusinessId);
            
            return new GetAllInvoicesForBusinessResult
            {
                Success = false,
                Message = $"Error retrieving invoices: {ex.Message}"
            };
        }
    }

    private static IQueryable<Domain.Entities.InvoiceManagement.Invoice> ApplyOrdering(
        IQueryable<Domain.Entities.InvoiceManagement.Invoice> query, 
        string orderBy, 
        bool descending)
    {
        Expression<Func<Domain.Entities.InvoiceManagement.Invoice, object>> keySelector = orderBy.ToLower() switch
        {
            "irn" => i => i.Irn.Value,
            "issuedate" => i => i.IssueDate,
            "status" => i => i.InvoiceStatus,
            "invoicestatus" => i => i.InvoiceStatus,
            "createdat" => i => i.CreatedAt,
            "updatedat" => i => i.UpdatedAt ?? DateTimeOffset.MinValue,
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