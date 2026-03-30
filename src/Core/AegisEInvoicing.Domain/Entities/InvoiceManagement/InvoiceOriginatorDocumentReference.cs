using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents an originator document reference for an invoice
/// Used to reference the original/source documents
/// </summary>
public class InvoiceOriginatorDocumentReference : AuditableEntity
{
    /// <summary>
    /// Invoice Reference Number (IRN) of the referenced originator document
    /// </summary>
    public IRN Irn { get; private set; } = null!;

    /// <summary>
    /// Issue date of the referenced originator document
    /// </summary>
    public DateOnly IssueDate { get; private set; }

    /// <summary>
    /// Foreign key to the invoice this originator reference belongs to
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// Navigation property to the parent invoice
    /// </summary>
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceOriginatorDocumentReference() { } // EF Constructor

    #region Factory Methods

    /// <summary>
    /// Creates a new originator document reference
    /// </summary>
    public static InvoiceOriginatorDocumentReference Create(
        Guid invoiceId,
        IRN irn,
        DateOnly issueDate)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID cannot be empty", nameof(invoiceId));

        var originatorReference = new InvoiceOriginatorDocumentReference
        {
            InvoiceId = invoiceId,
            Irn = irn,
            IssueDate = issueDate
        };

        return originatorReference;
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
