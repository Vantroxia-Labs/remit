using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents a receipt document reference for an invoice
/// Used to reference receipt/acknowledgment documents
/// </summary>
public class InvoiceReceiptDocumentReference : AuditableEntity
{
    /// <summary>
    /// Invoice Reference Number (IRN) of the referenced receipt document
    /// </summary>
    public IRN Irn { get; private set; } = null!;

    /// <summary>
    /// Issue date of the referenced receipt document
    /// </summary>
    public DateOnly IssueDate { get; private set; }

    /// <summary>
    /// Foreign key to the invoice this receipt reference belongs to
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// Navigation property to the parent invoice
    /// </summary>
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceReceiptDocumentReference() { } // EF Constructor

    #region Factory Methods

    /// <summary>
    /// Creates a new receipt document reference
    /// </summary>
    public static InvoiceReceiptDocumentReference Create(
        Guid invoiceId,
        IRN irn,
        DateOnly issueDate)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID cannot be empty", nameof(invoiceId));

        var receiptReference = new InvoiceReceiptDocumentReference
        {
            InvoiceId = invoiceId,
            Irn = irn,
            IssueDate = issueDate
        };

        return receiptReference;
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
