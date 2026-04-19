using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Interswitch.Interfaces;
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
    private readonly IInterswitchHttpClient _interswitchHttpClient;

    public UpdateReceivedInvoicePaymentStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateReceivedInvoicePaymentStatusCommandHandler> logger,
        IInterswitchHttpClient interswitchHttpClient)
    {
        _context = context;
        _currentUser = currentUserService;
        _logger = logger;
        _interswitchHttpClient = interswitchHttpClient;
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

        if (status != "PAID" && status != "REJECTED")
            return UpdateReceivedInvoicePaymentStatusResult.BadRequest(
                "Only PAID or REJECTED statuses are allowed for received invoices as the buyer.");

        if (status == "PAID" && string.IsNullOrWhiteSpace(request.Reference))
            return UpdateReceivedInvoicePaymentStatusResult.BadRequest(
                "A payment reference is required when marking an invoice as Paid.");

        var updateResult = await _interswitchHttpClient.UpdateStatusAsync(
            new Interswitch.Models.Requests.UpdateStatus.UpdateStatusRequest
            {
                PaymentStatus = status,
                Reference = request.Reference,
                IRN = receivedInvoice.Irn.Value
            }, cancellationToken);

        if (!updateResult.IsSuccess)
            return UpdateReceivedInvoicePaymentStatusResult.Failure(
                "Failed to update payment status with the regulator. Please try again.");

        receivedInvoice.UpdatePaymentStatus(status, request.Reference);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Received invoice payment status updated to {Status} for ID: {InvoiceId}",
            status, receivedInvoice.Id);

        return UpdateReceivedInvoicePaymentStatusResult.Updated(
            "Received invoice payment status updated successfully");
    }
}
