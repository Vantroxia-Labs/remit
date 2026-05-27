using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Commands.UpdateReceivedInvoicePaymentStatus;

public class UpdateReceivedInvoicePaymentStatusCommandHandler
    : IRequestHandler<UpdateReceivedInvoicePaymentStatusCommand, UpdateReceivedInvoicePaymentStatusResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateReceivedInvoicePaymentStatusCommandHandler> _logger;
    private readonly IAppProviderRouter _appProviderRouter;

    public UpdateReceivedInvoicePaymentStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateReceivedInvoicePaymentStatusCommandHandler> logger,
        IAppProviderRouter appProviderRouter)
    {
        _context = context;
        _currentUser = currentUserService;
        _logger = logger;
        _appProviderRouter = appProviderRouter;
    }

    public async Task<UpdateReceivedInvoicePaymentStatusResult> Handle(
        UpdateReceivedInvoicePaymentStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.BusinessId.HasValue)
            return UpdateReceivedInvoicePaymentStatusResult.AuthorizationError();

        var businessId = _currentUser.BusinessId.Value;

        var business = await _context.Businesses
            .Where(b => b.Id == businessId)
            .FirstOrDefaultAsync(cancellationToken);

        if (business is null)
            return UpdateReceivedInvoicePaymentStatusResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

        var receivedInvoice = await _context.ReceivedInvoices
            .Where(ri => ri.Id == request.ReceivedInvoiceId && ri.BusinessId == businessId)
            .FirstOrDefaultAsync(cancellationToken);

        if (receivedInvoice is null)
            return UpdateReceivedInvoicePaymentStatusResult.NotFound("Received invoice not found");

        var status = request.PaymentStatus.Trim().ToUpperInvariant();

        if (status != "PAID" && status != "REJECTED" && status != "PARTIAL")
            return UpdateReceivedInvoicePaymentStatusResult.BadRequest(
                "Only PAID, REJECTED, or PARTIAL statuses are allowed for received invoices as the buyer.");

        if (status == "PAID" && string.IsNullOrWhiteSpace(request.Reference))
            return UpdateReceivedInvoicePaymentStatusResult.BadRequest(
                "A payment reference is required when marking an invoice as Paid.");

        if (status == "PARTIAL" && request.Amount is null)
            return UpdateReceivedInvoicePaymentStatusResult.BadRequest(
                "An amount is required when marking an invoice as Partial.");

        var provider = await _appProviderRouter.GetProviderAsync(businessId, cancellationToken);
        var updateResult = await provider.UpdateStatusAsync(
            receivedInvoice.Irn.Value,
            status,
            request.Reference,
            request.Amount,
            cancellationToken);

        if (!updateResult.IsSuccess)
            return UpdateReceivedInvoicePaymentStatusResult.Failure(
                "Failed to update payment status with the regulator. Please try again.");

        receivedInvoice.UpdatePaymentStatus(status, request.Reference, request.Amount);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Received invoice payment status updated to {Status} for ID: {InvoiceId}",
            status, receivedInvoice.Id);

        var successMessage = status == "PARTIAL"
            ? "Received invoice payment status updated successfully. Once the invoice is fully paid, the payment status should be updated to PAID and the amount should be removed."
            : "Received invoice payment status updated successfully";

        return UpdateReceivedInvoicePaymentStatusResult.Updated(successMessage);
    }
}
