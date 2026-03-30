using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents a contract document reference for an invoice
/// Used to reference contract/agreement documents
/// </summary>
public class InvoiceContractDocumentReference : AuditableEntity
{
    /// <summary>
    /// Invoice Reference Number (IRN) of the referenced contract document
    /// </summary>
    public IRN Irn { get; private set; } = null!;

    /// <summary>
    /// Issue date of the referenced contract document
    /// </summary>
    public DateOnly IssueDate { get; private set; }

    /// <summary>
    /// Foreign key to the invoice this contract reference belongs to
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// Navigation property to the parent invoice
    /// </summary>
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceContractDocumentReference() { } // EF Constructor

    #region Factory Methods

    /// <summary>
    /// Creates a new contract document reference
    /// </summary>
    public static InvoiceContractDocumentReference Create(
        Guid invoiceId,
        IRN irn,
        DateOnly issueDate)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID cannot be empty", nameof(invoiceId));

        var contractReference = new InvoiceContractDocumentReference
        {
            InvoiceId = invoiceId,
            Irn = irn,
            IssueDate = issueDate
        };

        return contractReference;
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
