using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for handling invoice approval/rejection operations
/// </summary>
public interface IInvoiceApprovalService
{
    /// <summary>
    /// Validates that the current user can perform approval operations
    /// </summary>
    /// <returns>Validation result with error message if failed</returns>
    InvoiceApprovalValidationResult ValidateUserAuthorization();

    /// <summary>
    /// Fetches an invoice for approval operations with business isolation
    /// </summary>
    Task<Invoice?> GetInvoiceForApprovalAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that an invoice can be approved or rejected
    /// </summary>
    InvoiceApprovalValidationResult ValidateInvoiceForApproval(Invoice? invoice, Guid invoiceId);

    /// <summary>
    /// Processes invoice approval with transaction support
    /// </summary>
    Task<InvoiceApprovalOperationResult> ApproveInvoiceAsync(
        Invoice invoice,
        string? approvalComments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes invoice rejection with transaction support
    /// </summary>
    Task<InvoiceApprovalOperationResult> RejectInvoiceAsync(
        Invoice invoice,
        string rejectionReason,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of validation operations
/// </summary>
public record InvoiceApprovalValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public HttpStatusCodes StatusCode { get; init; } = HttpStatusCodes.OK;

    public static InvoiceApprovalValidationResult Success() => new() { IsValid = true };

    public static InvoiceApprovalValidationResult Unauthorized(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        StatusCode = HttpStatusCodes.Forbidden
    };

    public static InvoiceApprovalValidationResult NotFound(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        StatusCode = HttpStatusCodes.NotFound
    };

    public static InvoiceApprovalValidationResult BadRequest(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        StatusCode = HttpStatusCodes.BadRequest
    };
}

/// <summary>
/// Result of approval/rejection operations
/// </summary>
public record InvoiceApprovalOperationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? InvoiceId { get; init; }
    public string? Irn { get; init; }

    public static InvoiceApprovalOperationResult Success(Guid invoiceId, string irn) => new()
    {
        IsSuccess = true,
        InvoiceId = invoiceId,
        Irn = irn
    };

    public static InvoiceApprovalOperationResult Failure(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
}
