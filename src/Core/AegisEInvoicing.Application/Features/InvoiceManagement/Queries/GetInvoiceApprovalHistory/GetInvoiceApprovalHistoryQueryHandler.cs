using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceApprovalHistory;

public class GetInvoiceApprovalHistoryQueryHandler : IRequestHandler<GetInvoiceApprovalHistoryQuery, PaginatedList<InvoiceApprovalHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetInvoiceApprovalHistoryQueryHandler> _logger;

    public GetInvoiceApprovalHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ILogger<GetInvoiceApprovalHistoryQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PaginatedList<InvoiceApprovalHistoryDto>> Handle(GetInvoiceApprovalHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting GetInvoiceApprovalHistory query. User: IsPlatformAdmin={IsPlatformAdmin}, BusinessId={BusinessId}", 
            _currentUserService.IsPlatformAdmin, _currentUserService.BusinessId);

        // Start with base query including necessary navigation properties
        var baseQuery = _context.InvoiceApprovalHistories
            .Include(i => i.CreatedByUser)
            .Include(i => i.Invoice)
            .AsQueryable();

        // Log total count before filtering
        var totalRecordsInTable = await _context.InvoiceApprovalHistories.CountAsync(cancellationToken);
        _logger.LogInformation("Total records in InvoiceApprovalHistories table: {TotalRecords}", totalRecordsInTable);

        // Apply user-based filtering with more inclusive logic
        IQueryable<AegisEInvoicing.Domain.Entities.InvoiceManagement.InvoiceApprovalHistory> filteredQuery;
        
        if (_currentUserService.IsPlatformAdmin)
        {
            // Platform admins can see all records
            filteredQuery = baseQuery;
            _logger.LogInformation("Platform admin - showing all records");
        }
        else if (_currentUserService.BusinessId.HasValue)
        {
            // Business users can see records related to their business
            filteredQuery = baseQuery.Where(i => i.Invoice.BusinessId == _currentUserService.BusinessId.Value);
            _logger.LogInformation("Business user - filtering by BusinessId: {BusinessId}", _currentUserService.BusinessId.Value);
        }
        else
        {
            // If no specific business and not platform admin, show all records (or implement other logic)
            filteredQuery = baseQuery;
            _logger.LogWarning("No specific filtering applied - user has no BusinessId and is not platform admin");
        }

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        _logger.LogInformation("Total records after filtering: {FilteredCount}", totalCount);

        if (totalCount == 0)
        {
            _logger.LogWarning("No records found after filtering. Returning empty result.");
            return new PaginatedList<InvoiceApprovalHistoryDto>(new List<InvoiceApprovalHistoryDto>(), 0, request.PageNumber, request.PageSize);
        }

        // Apply pagination
        var paginatedQuery = filteredQuery
            .OrderByDescending(f => f.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        // Project to DTO with null safety
        var approvalHistory = await paginatedQuery.Select(f =>
            new InvoiceApprovalHistoryDto(
                f.Id,
                f.Invoice.Irn.Value,
                f.InvoiceStatus,
                f.CreatedByUser != null ? (f.CreatedByUser.FirstName ?? "") + " " + (f.CreatedByUser.LastName ?? "") : "System User",
                f.CreatedAt,
                f.CreatedBy,
                f.Invoice != null ? f.Invoice.InvoiceStatus : AegisEInvoicing.Domain.Enums.InvoiceStatus.DRAFT,
                f.Comments,
                f.Invoice != null ? f.Invoice.FIRSSubmissionId : null)
            ).ToListAsync(cancellationToken);

        _logger.LogInformation("Successfully retrieved {RecordCount} approval history records", approvalHistory.Count);

        return new PaginatedList<InvoiceApprovalHistoryDto>(approvalHistory, totalCount, request.PageNumber, request.PageSize);
    }
}
