using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceItem;

public class DeleteInvoiceItemCommandHandler : IRequestHandler<DeleteInvoiceItemCommand, DeleteInvoiceItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteInvoiceItemCommandHandler> _logger;

    public DeleteInvoiceItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteInvoiceItemCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DeleteInvoiceItemResult> Handle(DeleteInvoiceItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invoiceItem = await _context.InvoiceItems
                .Include(ii => ii.Invoice)
                .FirstOrDefaultAsync(ii => ii.Id == request.InvoiceItemId, cancellationToken);

            if (invoiceItem == null)
            {
                return new DeleteInvoiceItemResult
                {
                    Success = false,
                    Message = "Invoice item not found"
                };
            }

            if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            {
                if (invoiceItem.Invoice.BusinessId != _currentUserService.BusinessId.Value)
                {
                    return new DeleteInvoiceItemResult
                    {
                        Success = false,
                        Message = "Access denied to this invoice item"
                    };
                }
            }

            var invoice = invoiceItem.Invoice;
            invoice.RemoveInvoiceItem(request.InvoiceItemId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice item deleted successfully with ID: {InvoiceItemId} from Invoice: {InvoiceId}", 
                request.InvoiceItemId, invoiceItem.InvoiceId);

            return new DeleteInvoiceItemResult
            {
                Success = true,
                Message = "Invoice item deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice item with ID: {InvoiceItemId}", request.InvoiceItemId);
            return new DeleteInvoiceItemResult
            {
                Success = false,
                Message = $"Error deleting invoice item: {ex.Message}"
            };
        }
    }
}