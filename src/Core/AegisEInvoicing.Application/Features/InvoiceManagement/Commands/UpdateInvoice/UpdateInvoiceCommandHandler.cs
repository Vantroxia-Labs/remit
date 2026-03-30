using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoice;

public class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand, UpdateInvoiceResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateInvoiceCommandHandler> _logger;

    public UpdateInvoiceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateInvoiceCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateInvoiceResult> Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceLine)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                return new UpdateInvoiceResult
                {
                    Success = false,
                    Message = "Invoice not found"
                };
            }

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                invoice.UpdateNote(request.Note);
            }

            if (request.PaymentMeans is not null)
            {
                invoice.UpdatePaymentMeans(request.PaymentMeans);
            }                

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice updated successfully with ID: {InvoiceId}", invoice.Id);

            return new UpdateInvoiceResult
            {
                Success = true,
                Message = "Invoice updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice");
            return new UpdateInvoiceResult
            {
                Success = false,
                Message = $"Error updating invoice: {ex.Message}"
            };
        }
    }
}