using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for validating invoice references (IRNs) efficiently
/// </summary>
public interface IInvoiceReferenceValidator
{
    /// <summary>
    /// Validates that a single IRN exists in the database for the specified business
    /// </summary>
    /// <param name="irn">The IRN to validate</param>
    /// <param name="issueDate">The issue date of the referenced invoice</param>
    /// <param name="businessId">The business ID to validate against</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple with validation result and error message if invalid</returns>
    Task<(bool IsValid, string ErrorMessage)> ValidateIrnExistsAsync(
        string irn,
        DateOnly issueDate,
        Guid businessId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates multiple IRNs in a single database query for efficiency
    /// Useful for batch operations
    /// </summary>
    /// <param name="references">Collection of IRN and issue date pairs to validate</param>
    /// <param name="businessId">The business ID to validate against</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dictionary mapping IRN to existence status</returns>
    Task<Dictionary<string, bool>> ValidateMultipleIrnsAsync(
        IEnumerable<(string Irn, DateOnly IssueDate)> references,
        Guid businessId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates all document references for a single invoice request
    /// </summary>
    /// <param name="billingReferences">Billing references to validate</param>
    /// <param name="dispatchReference">Dispatch document reference to validate</param>
    /// <param name="receiptReference">Receipt document reference to validate</param>
    /// <param name="originatorReference">Originator document reference to validate</param>
    /// <param name="contractReference">Contract document reference to validate</param>
    /// <param name="additionalReferences">Additional document references to validate</param>
    /// <param name="businessId">The business ID to validate against</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple with validation result, error message, and list of invalid references</returns>
    Task<(bool IsValid, string ErrorMessage, List<string> InvalidReferences)> ValidateInvoiceReferencesAsync(
        List<CreateBillingReferenceDto>? billingReferences,
        CreateDocumentReferenceDto? dispatchReference,
        CreateDocumentReferenceDto? receiptReference,
        CreateDocumentReferenceDto? originatorReference,
        CreateDocumentReferenceDto? contractReference,
        List<CreateDocumentReferenceDto>? additionalReferences,
        Guid businessId,
        CancellationToken ct = default);
}
