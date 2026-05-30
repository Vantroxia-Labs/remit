using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoicePaymentStatus;

public class UpdateInvoicePaymentStatusCommandHandler : IRequestHandler<UpdateInvoicePaymentStatusCommand, UpdateInvoicePaymentStatusResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateInvoicePaymentStatusCommandHandler> _logger;
    private readonly IAppProviderRouter _appProviderRouter;

    public UpdateInvoicePaymentStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateInvoicePaymentStatusCommandHandler> logger,
        IAppProviderRouter appProviderRouter)
    {
        _context = context;
        _currentUser = currentUserService;
        _logger = logger;
        _appProviderRouter = appProviderRouter;
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

        if (request.PaymentStatus == PaymentStatus.Paid && string.IsNullOrWhiteSpace(request.Reference))
            return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.BadRequest("A payment reference is required when marking an invoice as Paid.");

        if (request.PaymentStatus == PaymentStatus.Partial && request.Amount is null)
            return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.BadRequest("An amount is required when marking an invoice as Partial.");

        var provider = await _appProviderRouter.GetProviderAsync(businessId, cancellationToken);
        var updatePaymentStatus = await provider.UpdateStatusAsync(
            invoice.Irn.Value,
            FormatPaymentStatus(request.PaymentStatus),
            request.Reference,
            request.Amount,
            cancellationToken);

        if (!updatePaymentStatus.IsSuccess)
            return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.Failure("Failed to update payment status with the regulator. Please try again.");

        invoice.UpdatePaymentStatus(request.PaymentStatus, request.Reference);

        if (request.PaymentStatus == PaymentStatus.Partial)
        {
            var payment = InvoicePayment.ForInvoice(invoice.Id, request.Amount!.Value, request.Reference, businessId);
            await _context.InvoicePayments.AddAsync(payment, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice payment status updated successfully with ID: {InvoiceId}", invoice.Id);

        var successMessage = request.PaymentStatus == PaymentStatus.Partial
            ? $"{ResponseMessages.INVOICE_UPDATED_SUCCESS} Once the invoice is fully paid, the payment status should be updated to PAID and the amount should be removed."
            : ResponseMessages.INVOICE_UPDATED_SUCCESS;

        return (UpdateInvoicePaymentStatusResult)UpdateInvoicePaymentStatusResult.Updated(successMessage);
    }

    private bool IsUserAuthorized() =>
      _currentUser.BusinessId.HasValue;

    private static string FormatPaymentStatus(PaymentStatus paymentStatus)
    {
        return paymentStatus switch
        {
            PaymentStatus.Paid => "PAID",
            PaymentStatus.Rejected => "REJECTED",
            PaymentStatus.Partial => "PARTIAL",
            _ => "PENDING"
        };
    }
}
