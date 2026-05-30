using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoices;

/// <summary>
/// Handler for retrieving received invoices with pagination and filtering
/// </summary>
public sealed class GetReceivedInvoicesQueryHandler : IRequestHandler<GetReceivedInvoicesQuery, GetReceivedInvoicesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetReceivedInvoicesQueryHandler> _logger;

    public GetReceivedInvoicesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetReceivedInvoicesQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetReceivedInvoicesResult> Handle(GetReceivedInvoicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Determine which business to query for
            var businessId = _currentUser.BusinessId;

            if (!businessId.HasValue)
            {
                _logger.LogWarning("No business ID provided and user has no associated business");
                return new GetReceivedInvoicesResult
                {
                    Success = false,
                    Message = "Business ID is required"
                };
            }

            _logger.LogInformation(
                "Retrieving received invoices for business {BusinessId} with filters:, Page={Page}",
                businessId.Value, request.Page);

            // Build query with filters
            var query = _context.ReceivedInvoices
                .Include(ri => ri.Business)
                .Where(ri => !ri.IsDeleted && ri.BusinessId == businessId.Value);

            // TODO: Filter by EnvironmentMode once property is added to ReceivedInvoice entity
            // if (request.EnvironmentMode.HasValue)
            // {
            //     query = query.Where(ri => ri.EnvironmentMode == request.EnvironmentMode.Value);
            // }

            if (request.StartDate.HasValue)
            {
                query = query.Where(ri => ri.IssueDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(ri => ri.IssueDate <= request.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(ri =>
                    ri.Irn.Value.ToLower().Contains(searchTerm) ||
                    ri.SupplierPartyName.ToLower().Contains(searchTerm) ||
                    ri.CustomerPartyName.ToLower().Contains(searchTerm));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, request.SortBy, request.SortDirection);

            // Apply pagination
            var pageSize = request.PageSize > 0 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;
            var skip = (page - 1) * pageSize;

            // Use lightweight DTO for list view (no address details, no JSON fields, fewer properties)
            var invoices = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(ri => new ReceivedInvoiceListDto
                {
                    Id = ri.Id,
                    IRN = ri.Irn.Value,
                    InvoiceTypeCode = ri.InvoiceTypeCode,
                    IssueDate = ri.IssueDate,
                    DueDate = ri.DueDate,
                    DocumentCurrencyCode = ri.DocumentCurrencyCode,
                    PaymentStatus = ri.PaymentStatus,
                    EntryStatus = ri.EntryStatus,
                    SupplierPartyName = ri.SupplierPartyName,
                    SupplierTIN = ri.SupplierTIN.Value,
                    CustomerPartyName = ri.CustomerPartyName,
                    CustomerTIN = ri.CustomerTIN.Value,
                    TaxInclusiveAmount = ri.TaxInclusiveAmount,
                    PayableAmount = ri.PayableAmount,
                    PaidAmount = ri.PaidAmount,
                    BusinessId = ri.BusinessId,
                    BusinessName = ri.Business != null ? ri.Business.Name : null,
                    IsReconciled = ri.IsReconciled,
                    ReconciledAt = ri.ReconciledAt,
                    CreatedAt = ri.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation(
                "Retrieved {Count} received invoices for business {BusinessId} (Page {Page}/{TotalPages})",
                invoices.Count, businessId.Value, page, totalPages);

            return new GetReceivedInvoicesResult
            {
                Success = true,
                Message = $"Retrieved {invoices.Count} invoices",
                Invoices = invoices,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving received invoices: {Message}", ex.Message);

            return new GetReceivedInvoicesResult
            {
                Success = false,
                Message = "An error occurred while retrieving invoices"
            };
        }
    }

    private static IQueryable<Domain.Entities.InvoiceManagement.ReceivedInvoice> ApplySorting(
        IQueryable<Domain.Entities.InvoiceManagement.ReceivedInvoice> query,
        string sortBy,
        string sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "issuedate" => isDescending
                ? query.OrderByDescending(ri => ri.IssueDate)
                : query.OrderBy(ri => ri.IssueDate),
            "suppliername" => isDescending
                ? query.OrderByDescending(ri => ri.SupplierPartyName)
                : query.OrderBy(ri => ri.SupplierPartyName),
            "payableamount" => isDescending
                ? query.OrderByDescending(ri => ri.PayableAmount)
                : query.OrderBy(ri => ri.PayableAmount),
            "paymentstatus" => isDescending
                ? query.OrderByDescending(ri => ri.PaymentStatus)
                : query.OrderBy(ri => ri.PaymentStatus),
            "createdat" => isDescending
                ? query.OrderByDescending(ri => ri.CreatedAt)
                : query.OrderBy(ri => ri.CreatedAt),
            _ => query.OrderByDescending(ri => ri.IssueDate) // Default sort
        };
    }
}
