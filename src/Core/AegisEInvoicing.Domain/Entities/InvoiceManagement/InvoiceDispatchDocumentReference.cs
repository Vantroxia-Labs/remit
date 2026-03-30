using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents a dispatch document reference for an invoice
/// Used to reference dispatch/shipping documents
/// </summary>
public class InvoiceDispatchDocumentReference : AuditableEntity
{
    /// <summary>
    /// Invoice Reference Number (IRN) of the referenced dispatch document
    /// </summary>
    public IRN Irn { get; private set; } = null!;

    /// <summary>
    /// Issue date of the referenced dispatch document
    /// </summary>
    public DateOnly IssueDate { get; private set; }

    /// <summary>
    /// Foreign key to the invoice this dispatch reference belongs to
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// Navigation property to the parent invoice
    /// </summary>
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceDispatchDocumentReference() { } // EF Constructor

    #region Factory Methods

    /// <summary>
    /// Creates a new dispatch document reference
    /// </summary>
    public static InvoiceDispatchDocumentReference Create(
        Guid invoiceId,
        IRN irn,
        DateOnly issueDate)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID cannot be empty", nameof(invoiceId));

        var dispatchReference = new InvoiceDispatchDocumentReference
        {
            InvoiceId = invoiceId,
            Irn = irn,
            IssueDate = issueDate
        };

        return dispatchReference;
    }

    #endregion

    #region Business Logic Methods

    /// <summary>
    /// Updates the IRN of the dispatch reference
    /// </summary>
    public void UpdateIrn(IRN irn)
    {
        if (irn == null)
            throw new ArgumentNullException(nameof(irn));

        Irn = irn;
    }

    /// <summary>
    /// Updates the issue date of the dispatch reference
    /// </summary>
    public void UpdateIssueDate(DateOnly issueDate)
    {
        IssueDate = issueDate;
    }

    #endregion
}