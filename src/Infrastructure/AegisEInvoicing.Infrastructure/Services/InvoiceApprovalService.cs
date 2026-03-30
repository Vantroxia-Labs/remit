using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Service for handling invoice approval/rejection operations
/// </summary>
public sealed class InvoiceApprovalService : IInvoiceApprovalService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<InvoiceApprovalService> _logger;

    public InvoiceApprovalService(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<InvoiceApprovalService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public InvoiceApprovalValidationResult ValidateUserAuthorization()
    {
        if (!_currentUser.HasRole(RoleConstants.ClientAdmin))
        {
            _logger.LogWarning(
                "User {UserId} attempted approval operation without ClientAdmin role",
                _currentUser.UserId);
            return InvoiceApprovalValidationResult.Unauthorized(
                "Only ClientAdmin users can approve or reject invoices");
        }

        if (!_currentUser.BusinessId.HasValue)
        {
            _logger.LogWarning(
                "User {UserId} attempted approval operation without BusinessId",
                _currentUser.UserId);
            return InvoiceApprovalValidationResult.Unauthorized(
                "User is not associated with a business");
        }

        return InvoiceApprovalValidationResult.Success();
    }

    public async Task<Invoice?> GetInvoiceForApprovalAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(i => i.Id == invoiceId && i.BusinessId == _currentUser.BusinessId!.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public InvoiceApprovalValidationResult ValidateInvoiceForApproval(Invoice? invoice, Guid invoiceId)
    {
        if (invoice is null)
        {
            _logger.LogWarning(
                "Invoice {InvoiceId} not found for business {BusinessId}",
                invoiceId, _currentUser.BusinessId);
            return InvoiceApprovalValidationResult.NotFound(
                $"Invoice with ID {invoiceId} not found");
        }

        if (invoice.InvoiceStatus != InvoiceStatus.PENDING_APPROVAL)
        {
            _logger.LogWarning(
                "Invoice {InvoiceId} cannot be processed. Current status: {Status}",
                invoiceId, invoice.InvoiceStatus);
            return InvoiceApprovalValidationResult.BadRequest(
                $"Invoice cannot be approved or rejected. Current status: {invoice.InvoiceStatus}. " +
                "Only invoices with PENDING_APPROVAL status can be processed.");
        }

        return InvoiceApprovalValidationResult.Success();
    }

    public async Task<InvoiceApprovalOperationResult> ApproveInvoiceAsync(
        Invoice invoice,
        string? approvalComments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            invoice.UpdateStatus(InvoiceStatus.APPROVED);

            var comments = string.IsNullOrWhiteSpace(approvalComments)
                ? $"Invoice approved by ClientAdmin {_currentUser.UserName}"
                : $"Invoice approved by ClientAdmin {_currentUser.UserName}. Comments: {approvalComments}";

            var approvalHistory = InvoiceApprovalHistory.Create(
                invoice.Id,
                InvoiceStatus.APPROVED,
                comments);

            await _context.InvoiceApprovalHistories.AddAsync(approvalHistory, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Invoice {InvoiceId} approved by ClientAdmin {UserId}. IRN: {Irn}",
                invoice.Id, _currentUser.UserId, invoice.Irn.Value);

            return InvoiceApprovalOperationResult.Success(invoice.Id, invoice.Irn.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving invoice {InvoiceId}", invoice.Id);
            return InvoiceApprovalOperationResult.Failure("An error occurred while approving the invoice");
        }
    }

    public async Task<InvoiceApprovalOperationResult> RejectInvoiceAsync(
        Invoice invoice,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            invoice.UpdateStatus(InvoiceStatus.REJECTED);

            var comments = $"Invoice rejected by ClientAdmin {_currentUser.UserName}. Reason: {rejectionReason}";

            var rejectionHistory = InvoiceApprovalHistory.Create(
                invoice.Id,
                InvoiceStatus.REJECTED,
                comments);

            await _context.InvoiceApprovalHistories.AddAsync(rejectionHistory, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Invoice {InvoiceId} rejected by ClientAdmin {UserId}. IRN: {Irn}. Reason: {Reason}",
                invoice.Id, _currentUser.UserId, invoice.Irn.Value, rejectionReason);

            return InvoiceApprovalOperationResult.Success(invoice.Id, invoice.Irn.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invoice {InvoiceId}", invoice.Id);
            return InvoiceApprovalOperationResult.Failure("An error occurred while rejecting the invoice");
        }
    }
}
