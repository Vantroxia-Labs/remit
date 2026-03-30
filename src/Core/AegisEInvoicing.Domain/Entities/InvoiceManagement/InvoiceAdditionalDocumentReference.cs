using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents an additional document reference for an invoice
/// Used to reference any other supporting documents
/// This is a collection type - multiple additional documents can be referenced
/// </summary>
public class InvoiceAdditionalDocumentReference : AuditableEntity
{
    /// <summary>
    /// Invoice Reference Number (IRN) of the referenced additional document
    /// </summary>
    public IRN Irn { get; private set; } = null!;

    /// <summary>
    /// Issue date of the referenced additional document
    /// </summary>
    public DateOnly IssueDate { get; private set; }

    /// <summary>
    /// Foreign key to the invoice this additional reference belongs to
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// Navigation property to the parent invoice
    /// </summary>
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceAdditionalDocumentReference() { } // EF Constructor

    #region Factory Methods

    /// <summary>
    /// Creates a new additional document reference
    /// </summary>
    public static InvoiceAdditionalDocumentReference Create(
        Guid invoiceId,
        IRN irn,
        DateOnly issueDate)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID cannot be empty", nameof(invoiceId));

        var additionalReference = new InvoiceAdditionalDocumentReference
        {
            InvoiceId = invoiceId,
            Irn = irn,
            IssueDate = issueDate
        };

        return additionalReference;
    }

    #endregion

    #region Business Logic Methods

    public void UpdateIrn(IRN irn)
    {
        if (irn == null)
            throw new ArgumentNullException(nameof(irn));

        Irn = irn;
    }

    public void UpdateIssueDate(DateOnly issueDate)
    {
        IssueDate = issueDate;
    }

    #endregion
}
