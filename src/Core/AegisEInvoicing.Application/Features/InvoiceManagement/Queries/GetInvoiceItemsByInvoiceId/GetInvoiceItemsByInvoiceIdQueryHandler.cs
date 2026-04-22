using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceItemsByInvoiceId;

public class GetInvoiceItemsByInvoiceIdQueryHandler : IRequestHandler<GetInvoiceItemsByInvoiceIdQuery, GetInvoiceItemsByInvoiceIdResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetInvoiceItemsByInvoiceIdQueryHandler> _logger;

    public GetInvoiceItemsByInvoiceIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetInvoiceItemsByInvoiceIdQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetInvoiceItemsByInvoiceIdResult> Handle(GetInvoiceItemsByInvoiceIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var invoiceQuery = _context.Invoices
                .AsNoTracking()
                .Where(i => i.Id == request.InvoiceId);

            if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            {
                invoiceQuery = invoiceQuery.Where(i => i.BusinessId == _currentUserService.BusinessId.Value);
            }

            var invoiceExists = await invoiceQuery.AnyAsync(cancellationToken);

            if (!invoiceExists)
            {
                return new GetInvoiceItemsByInvoiceIdResult
                {
                    Success = false,
                    Message = "Invoice not found"
                };
            }

            var invoiceItems = await _context.InvoiceItems
                .Include(li => li.BusinessItem)
                .Where(ii => ii.InvoiceId == request.InvoiceId)
                .Select(item => new InvoiceItemDto
                {
                    Id = item.Id,
                    InvoiceId = item.InvoiceId,
                    ItemCode = item.BusinessItem!.ItemId,
                    Category = item.BusinessItem!.ServiceCode.Name ?? "",
                    ItemDescription = item.BusinessItem!.ItemDescription,
                    UnitPrice = item.BusinessItem!.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.Quantity * item.BusinessItem!.UnitPrice
                })
                .OrderBy(ii => ii.Id)
                .ToListAsync(cancellationToken);

            return new GetInvoiceItemsByInvoiceIdResult
            {
                Success = true,
                Message = "Invoice items retrieved successfully",
                InvoiceItems = invoiceItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice items for Invoice ID: {InvoiceId}", request.InvoiceId);
            return new GetInvoiceItemsByInvoiceIdResult
            {
                Success = false,
                Message = $"Error retrieving invoice items: {ex.Message}"
            };
        }
    }
}