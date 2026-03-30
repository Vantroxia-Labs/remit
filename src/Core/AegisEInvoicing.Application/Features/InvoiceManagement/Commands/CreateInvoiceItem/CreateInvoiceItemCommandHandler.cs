using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceItem;

public class CreateInvoiceItemCommandHandler : IRequestHandler<CreateInvoiceItemCommand, CreateInvoiceItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateInvoiceItemCommandHandler> _logger;

    public CreateInvoiceItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateInvoiceItemCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CreateInvoiceItemResult> Handle(CreateInvoiceItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceLine)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                return new CreateInvoiceItemResult
                {
                    Success = false,
                    Message = "Invoice not found"
                };
            }

            if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            {
                if (invoice.BusinessId != _currentUserService.BusinessId.Value)
                {
                    return new CreateInvoiceItemResult
                    {
                        Success = false,
                        Message = "Access denied to this invoice"
                    };
                }
            }

            // Fetch the business item to get the current price for snapshot
            var businessItem = await _context.BusinessItems
                .FirstOrDefaultAsync(bi => bi.Id == request.BusinessItemId, cancellationToken);

            if (businessItem is null)
            {
                return new CreateInvoiceItemResult
                {
                    Success = false,
                    Message = "Business item not found"
                };
            }

            var invoiceItem = InvoiceItem.Create(
                request.BusinessItemId,
                request.InvoiceId,
                request.Quantity,
                businessItem.UnitPrice, // Snapshot the current price
                request.DiscountFee == null ? null : DiscountFee.Create(request.DiscountFee.Amount, request.DiscountFee.Code),
                request.AdditionalFee == null ? null : AdditionalFee.Create(request.AdditionalFee.Amount, request.AdditionalFee.Code));

            invoice.AddInvoiceItem(invoiceItem);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice item created successfully with ID: {InvoiceItemId} for Invoice: {InvoiceId}",
                invoiceItem.Id, request.InvoiceId);

            return new CreateInvoiceItemResult
            {
                Success = true,
                InvoiceItemId = invoiceItem.Id,
                Message = "Invoice item created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice item for Invoice: {InvoiceId}", request.InvoiceId);
            return new CreateInvoiceItemResult
            {
                Success = false,
                Message = $"Error creating invoice item: {ex.Message}"
            };
        }
    }
}