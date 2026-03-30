using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoiceItem;

public class UpdateInvoiceItemCommandHandler : IRequestHandler<UpdateInvoiceItemCommand, UpdateInvoiceItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateInvoiceItemCommandHandler> _logger;

    public UpdateInvoiceItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateInvoiceItemCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateInvoiceItemResult> Handle(UpdateInvoiceItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invoiceItem = await _context.InvoiceItems
                .Include(ii => ii.Invoice)
                .FirstOrDefaultAsync(ii => ii.Id == request.InvoiceItemId, cancellationToken);

            if (invoiceItem == null)
            {
                return new UpdateInvoiceItemResult
                {
                    Success = false,
                    Message = "Invoice item not found"
                };
            }

            if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            {
                if (invoiceItem.Invoice.BusinessId != _currentUserService.BusinessId.Value)
                {
                    return new UpdateInvoiceItemResult
                    {
                        Success = false,
                        Message = "Access denied to this invoice item"
                    };
                }
            }

            if (request.Quantity.HasValue)
            {
                invoiceItem.UpdateQuantity(request.Quantity.Value);
            }

            if (request.DiscountFee is not null)
            {
                invoiceItem.UpdateDiscountFee(DiscountFee.Create(request.DiscountFee.Amount, request.DiscountFee.Code));
            }

            if (request.AdditionalFee is not null)
            {
                invoiceItem.UpdateAdditionalFee(AdditionalFee.Create(request.AdditionalFee.Amount, request.AdditionalFee.Code));
            }

            _context.InvoiceItems.Update(invoiceItem);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice item updated successfully with ID: {InvoiceItemId}", request.InvoiceItemId);

            return new UpdateInvoiceItemResult
            {
                Success = true,
                Message = "Invoice item updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice item with ID: {InvoiceItemId}", request.InvoiceItemId);
            return new UpdateInvoiceItemResult
            {
                Success = false,
                Message = $"Error updating invoice item: {ex.Message}"
            };
        }
    }
}