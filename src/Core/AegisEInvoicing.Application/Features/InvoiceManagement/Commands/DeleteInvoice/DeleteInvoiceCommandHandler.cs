using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoice;

public class DeleteInvoiceCommandHandler : IRequestHandler<DeleteInvoiceCommand, DeleteInvoiceResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteInvoiceCommandHandler> _logger;

    public DeleteInvoiceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteInvoiceCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DeleteInvoiceResult> Handle(DeleteInvoiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceLine)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                return new DeleteInvoiceResult
                {
                    Success = false,
                    Message = "Invoice not found"
                };
            }

            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return new DeleteInvoiceResult
                {
                    Success = false,
                    Message = "User not authenticated"
                };
            }

            var deletableStatuses = new[]
            {
                InvoiceStatus.DRAFT,
                InvoiceStatus.CREATED,
                InvoiceStatus.APPROVED,
                InvoiceStatus.VALIDATED,
                InvoiceStatus.VALIDATIONFAILED,
                InvoiceStatus.SIGNINGFAILED
            };

            if (!deletableStatuses.Contains(invoice.InvoiceStatus))
            {
                return new DeleteInvoiceResult
                {
                    Success = false,
                    Message = "Invoice cannot be deleted as it has been submitted to FIRS."
                };
            }

            invoice.MarkAsDeleted(userId);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice deleted successfully with ID: {InvoiceId} by user: {UserId}", 
                request.InvoiceId, userId.Value);

            return new DeleteInvoiceResult
            {
                Success = true,
                Message = "Invoice deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice with ID: {InvoiceId}", request.InvoiceId);
            return new DeleteInvoiceResult
            {
                Success = false,
                Message = $"Error deleting invoice: {ex.Message}"
            };
        }
    }
}