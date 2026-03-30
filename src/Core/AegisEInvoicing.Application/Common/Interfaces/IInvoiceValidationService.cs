using EInvoiceIntegrator.Domain.Entities;

namespace EInvoiceIntegrator.Application.Common.Interfaces;

/// <summary>
/// Service for validating invoice data against business rules and FIRS requirements
/// </summary>
public interface IInvoiceValidationService
{
    /// <summary>
    /// Validates an invoice for structural and business rule compliance
    /// </summary>
    /// <param name="invoice">The invoice to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors if any</returns>
    Task<InvoiceValidationResult> ValidateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates invoice items for completeness and accuracy
    /// </summary>
    /// <param name="invoiceItems">The invoice items to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors if any</returns>
    Task<InvoiceValidationResult> ValidateInvoiceItemsAsync(IEnumerable<InvoiceItem> invoiceItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates invoice against FIRS UBL format requirements
    /// </summary>
    /// <param name="invoice">The invoice to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with FIRS-specific errors if any</returns>
    Task<InvoiceValidationResult> ValidateForFIRSSubmissionAsync(Invoice invoice, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of invoice validation
/// </summary>
public class InvoiceValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();

    public static InvoiceValidationResult Success() => new() { IsValid = true };
    
    public static InvoiceValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    public static InvoiceValidationResult Warning(params string[] warnings) => new()
    {
        IsValid = true,
        Warnings = warnings.ToList()
    };
}