using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents a billing reference for an invoice
/// Used to reference previous invoices or related billing documents
/// </summary>
public class InvoiceBillingReference : AuditableEntity
{
    /// <summary>
    /// Invoice Reference Number (IRN) of the referenced document
    /// </summary>
    public IRN Irn { get; private set; } = null!;

    /// <summary>
    /// Issue date of the referenced billing document
    /// </summary>
    public DateOnly IssueDate { get; private set; }

    /// <summary>
    /// Foreign key to the invoice this billing reference belongs to
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// Navigation property to the parent invoice
    /// </summary>
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceBillingReference() { } // EF Constructor

    #region Factory Methods

    /// <summary>
    /// Creates a new billing reference
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice this reference belongs to</param>
    /// <param name="irn">The IRN of the referenced billing document</param>
    /// <param name="issueDate">The issue date of the referenced document</param>
    /// <returns>A new BillingReference instance</returns>
    public static InvoiceBillingReference Create(
        Guid invoiceId,
        IRN irn,
        DateOnly issueDate)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID cannot be empty", nameof(invoiceId));

        var billingReference = new InvoiceBillingReference
        {
            InvoiceId = invoiceId,
            Irn = irn,
            IssueDate = issueDate
        };

        return billingReference;
    }

    #endregion

    #region Business Logic Methods

    /// <summary>
    /// Updates the IRN of the billing reference
    /// </summary>
    public void UpdateIrn(IRN irn)
    {
        if (string.IsNullOrWhiteSpace(irn))
            throw new ArgumentException("IRN cannot be null or empty", nameof(irn));

        Irn = irn;
    }

    /// <summary>
    /// Updates the issue date of the billing reference
    /// </summary>
    public void UpdateIssueDate(DateOnly issueDate)
    {
        IssueDate = issueDate;
    }

    #endregion
}