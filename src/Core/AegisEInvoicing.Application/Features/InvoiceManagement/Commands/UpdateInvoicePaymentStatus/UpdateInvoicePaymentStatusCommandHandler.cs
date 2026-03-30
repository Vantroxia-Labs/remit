using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using AegisEInvoicing.Interswitch.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoicePaymentStatus;

public class UpdateInvoicePaymentStatusCommandHandler : IRequestHandler<UpdateInvoicePaymentStatusCommand, UpdateInvoicePaymentStatusResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateInvoicePaymentStatusCommandHandler> _logger;
    private readonly IInterswitchHttpClient _interswitchHttpClient;
    private readonly IEncryptionService _encryptionService;

    public UpdateInvoicePaymentStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateInvoicePaymentStatusCommandHandler> logger,
        IInterswitchHttpClient interswitchHttpClient,
        IEncryptionService encryptionService)
    {
        _context = context;
        _currentUser = currentUserService;
        _logger = logger;
        _interswitchHttpClient = interswitchHttpClient;
        _encryptionService = encryptionService;
    }

    public async Task<UpdateInvoicePaymentStatusResult> Handle(UpdateInvoicePaymentStatusCommand request, CancellationToken cancellationToken)
    {
        if (!IsUserAuthorized())
                return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.AuthorizationError();

            var businessId = _currentUser.BusinessId!.Value;

            var business = await _context.Businesses
                .Where(b => b.Id == businessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (business is null)
                return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var invoice = await _context.Invoices
                              .Where(i => i.Id == request.InvoiceId)
                              .FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
                return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND);

          
            var updatePaymentStatus = await _interswitchHttpClient.UpdateStatusAsync(
                new Interswitch.Models.Requests.UpdateStatus.UpdateStatusRequest
                {
                    PaymentStatus = FormatPaymentStatus(request.PaymentStatus),
                    Reference = invoice.PaymentReference,
                    IRN = invoice.Irn.Value
                }, cancellationToken);

            if (updatePaymentStatus.IsSuccess)
                invoice.UpdatePaymentStatus(request.PaymentStatus);
            else
                invoice.UpdatePaymentStatus(PaymentStatus.Failed);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice payment status updated successfully with ID: {InvoiceId}", invoice.Id);

        return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.Updated(ResponseMessages.INVOICE_UPDATED_SUCCESS);
    }

    private bool IsUserAuthorized() =>
      _currentUser.BusinessId.HasValue;

    private static string FormatPaymentStatus(PaymentStatus paymentStatus)
    {
        return paymentStatus switch
        {
            PaymentStatus.Paid => "PAID",
            PaymentStatus.Pending => "PENDING",
            PaymentStatus.Cancelled or PaymentStatus.Failed => "REJECTED",
            _ => "PENDING"
        };
    }
}
