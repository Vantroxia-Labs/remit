using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceItemById;

public class GetInvoiceItemByIdQueryHandler : IRequestHandler<GetInvoiceItemByIdQuery, GetInvoiceItemByIdResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetInvoiceItemByIdQueryHandler> _logger;

    public GetInvoiceItemByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetInvoiceItemByIdQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetInvoiceItemByIdResult> Handle(GetInvoiceItemByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.InvoiceItems
                .Include(ii => ii.Invoice)
                .Include(ii => ii.BusinessItem)
                .ThenInclude(bi => bi!.ItemCategory)
                .Where(ii => ii.Id == request.InvoiceItemId);

            if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            {
                query = query.Where(ii => ii.Invoice.BusinessId == _currentUserService.BusinessId.Value);
            }

            var invoiceItem = await query.FirstOrDefaultAsync(cancellationToken);

            if (invoiceItem == null)
            {
                return new GetInvoiceItemByIdResult
                {
                    Success = false,
                    Message = "Invoice item not found"
                };
            }

            var invoiceItemDto = new InvoiceItemDto
            {
                Id = invoiceItem.Id,
                InvoiceId = invoiceItem.InvoiceId,
                ItemCode = invoiceItem.BusinessItem!.ItemId,
                ServiceCode = invoiceItem.BusinessItem!.ServiceCode,
                Category = invoiceItem.BusinessItem!.ItemCategory!.Name,
                ItemDescription = invoiceItem.BusinessItem!.ItemDescription,
                DiscountFee = invoiceItem.DiscountFee,
                AdditionalFee = invoiceItem.AdditionalFee,
                UnitPrice = invoiceItem.BusinessItem!.UnitPrice,
                Quantity = invoiceItem.Quantity,
                TotalPrice = invoiceItem.Quantity * invoiceItem.BusinessItem!.UnitPrice
            };

            return new GetInvoiceItemByIdResult
            {
                Success = true,
                Message = "Invoice item retrieved successfully",
                InvoiceItem = invoiceItemDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice item with ID: {InvoiceItemId}", request.InvoiceItemId);
            return new GetInvoiceItemByIdResult
            {
                Success = false,
                Message = $"Error retrieving invoice item: {ex.Message}"
            };
        }
    }
}