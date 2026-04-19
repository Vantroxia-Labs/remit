using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents a purchase invoice (received invoice) from a supplier
/// This is an aggregate root that stores complete invoice data received from Interswitch
/// </summary>
public class ReceivedInvoice : AuditableAggregateRoot
{
    // Core Invoice Identification
    /// <summary>
    /// Invoice Reference Number (IRN) - Unique identifier from FIRS
    /// </summary>
    public IRN Irn { get; private set; } = null!;

    /// <summary>
    /// Invoice type code (e.g., 380 for commercial invoice)
    /// </summary>
    public string InvoiceTypeCode { get; private set; } = string.Empty;

    /// <summary>
    /// Date when invoice was issued
    /// </summary>
    public DateOnly IssueDate { get; private set; }

    /// <summary>
    /// Time when invoice was issued
    /// </summary>
    public string? IssueTime { get; private set; }

    /// <summary>
    /// Invoice due date for payment
    /// </summary>
    public DateOnly? DueDate { get; private set; }

    // Currency Information
    /// <summary>
    /// Currency code for the invoice (e.g., NGN)
    /// </summary>
    public string DocumentCurrencyCode { get; private set; } = string.Empty;

    /// <summary>
    /// Currency code for tax calculations
    /// </summary>
    public string TaxCurrencyCode { get; private set; } = string.Empty;

    // Status Information
    /// <summary>
    /// Payment status of the invoice
    /// </summary>
    public string PaymentStatus { get; private set; } = string.Empty;

    /// <summary>
    /// Entry status in FIRS system
    /// </summary>
    public string EntryStatus { get; private set; } = string.Empty;

    /// <summary>
    /// Date when invoice was synced from FIRS
    /// </summary>
    public string? SyncDate { get; private set; }

    // Supplier Information (Seller - who sent us the invoice)
    /// <summary>
    /// Supplier party name
    /// </summary>
    public string SupplierPartyName { get; private set; } = string.Empty;

    /// <summary>
    /// Supplier Tax Identification Number
    /// </summary>
    public TIN SupplierTIN { get; private set; } = null!;

    /// <summary>
    /// Supplier Business Registration Number
    /// </summary>
    public string? SupplierBRN { get; private set; }

    /// <summary>
    /// Supplier email address
    /// </summary>
    [EmailAddress]
    public string? SupplierEmail { get; private set; }

    /// <summary>
    /// Supplier phone number
    /// </summary>
    public string? SupplierTelephone { get; private set; }

    /// <summary>
    /// Supplier physical address
    /// </summary>
    public Address? SupplierAddress { get; private set; }

    // Customer Information (Buyer - our business receiving the invoice)
    /// <summary>
    /// Customer party name (our business)
    /// </summary>
    public string CustomerPartyName { get; private set; } = string.Empty;

    /// <summary>
    /// Customer Tax Identification Number (our TIN)
    /// </summary>
    public TIN CustomerTIN { get; private set; } = null!;

    /// <summary>
    /// Customer Business Registration Number
    /// </summary>
    public string? CustomerBRN { get; private set; }

    /// <summary>
    /// Customer email address
    /// </summary>
    [EmailAddress]
    public string? CustomerEmail { get; private set; }

    /// <summary>
    /// Customer phone number
    /// </summary>
    public string? CustomerTelephone { get; private set; }

    /// <summary>
    /// Customer physical address
    /// </summary>
    public Address? CustomerAddress { get; private set; }

    // Financial Amounts
    /// <summary>
    /// Total amount before tax
    /// </summary>
    public decimal LineExtensionAmount { get; private set; }

    /// <summary>
    /// Total amount excluding tax
    /// </summary>
    public decimal TaxExclusiveAmount { get; private set; }

    /// <summary>
    /// Total amount including tax
    /// </summary>
    public decimal TaxInclusiveAmount { get; private set; }

    /// <summary>
    /// Total tax amount
    /// </summary>
    public decimal TotalTaxAmount { get; private set; }

    /// <summary>
    /// Payable amount (final amount to pay)
    /// </summary>
    public decimal PayableAmount { get; private set; }

    /// <summary>
    /// Amount paid (if any)
    /// </summary>
    public decimal? PaidAmount { get; private set; }

    /// <summary>
    /// Payable rounding amount
    /// </summary>
    public decimal? PayableRoundingAmount { get; private set; }

    // Additional Information
    /// <summary>
    /// Additional notes or remarks on the invoice
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Buyer reference
    /// </summary>
    public string? BuyerReference { get; private set; }

    /// <summary>
    /// Payment reference (e.g., bank transfer ref, receipt number)
    /// </summary>
    public string? PaymentReference { get; private set; }

    /// <summary>
    /// Accounting cost
    /// </summary>
    public string? AccountingCost { get; private set; }

    /// <summary>
    /// Complete invoice lines data stored as JSON for detailed analysis
    /// </summary>
    public string? InvoiceLinesJson { get; private set; }

    /// <summary>
    /// Complete tax total data stored as JSON for detailed analysis
    /// </summary>
    public string? TaxTotalJson { get; private set; }

    public string FirsBusinessId { get; private set; } = null!;

    // Navigation Properties
    /// <summary>
    /// Foreign key to the Business entity (our business receiving the invoice)
    /// Populated by matching CustomerTIN to Business.TaxIdentificationNumber
    /// </summary>
    public Guid BusinessId { get; private set; }

    /// <summary>
    /// Navigation property to the business receiving this invoice
    /// </summary>
    public Business Business { get; private set; } = null!;

    /// <summary>
    /// Set when this invoice is included in a VAT schedule's input section.
    /// Prevents double-counting across schedule generations.
    /// </summary>
    public Guid? InputVatScheduleId { get; private set; }

    /// <summary>
    /// Set when this invoice is included in a WHT schedule.
    /// Prevents double-counting across schedule generations.
    /// </summary>
    public Guid? WhtScheduleId { get; private set; }

    /// <summary>
    /// Indicates whether this invoice has been reconciled/processed
    /// </summary>
    public bool IsReconciled { get; private set; }

    /// <summary>
    /// Date when invoice was reconciled
    /// </summary>
    public DateTimeOffset? ReconciledAt { get; private set; }

    /// <summary>
    /// User who reconciled the invoice
    /// </summary>
    public Guid? ReconciledBy { get; private set; }

    private ReceivedInvoice() { } // EF Constructor

    #region Factory Methods

    /// <summary>
    /// Creates a new ReceivedInvoice from Interswitch purchase invoice data
    /// </summary>
    public static ReceivedInvoice Create(
        Guid businessId,
        string firsBusinessId,
        IRN irn,
        string invoiceTypeCode,
        DateOnly issueDate,
        string documentCurrencyCode,
        string taxCurrencyCode,
        string paymentStatus,
        string entryStatus,
        string supplierPartyName,
        TIN supplierTIN,
        string customerPartyName,
        TIN customerTIN,
        decimal lineExtensionAmount,
        decimal taxExclusiveAmount,
        decimal taxInclusiveAmount,
        decimal totalTaxAmount,
        decimal payableAmount,
        Guid createdBy,
        string? issueTime = null,
        DateOnly? dueDate = null,
        string? syncDate = null,
        string? supplierBRN = null,
        string? supplierEmail = null,
        string? supplierTelephone = null,
        Address? supplierAddress = null,
        string? customerBRN = null,
        string? customerEmail = null,
        string? customerTelephone = null,
        Address? customerAddress = null,
        decimal? paidAmount = null,
        decimal? payableRoundingAmount = null,
        string? note = null,
        string? buyerReference = null,
        string? accountingCost = null,
        string? invoiceLinesJson = null,
        string? taxTotalJson = null)
    {
        ValidateInputs(irn, supplierTIN, customerTIN, invoiceTypeCode, documentCurrencyCode, supplierPartyName, customerPartyName);

        return new ReceivedInvoice
        {
            Irn = irn,
            InvoiceTypeCode = invoiceTypeCode,
            IssueDate = issueDate,
            IssueTime = issueTime,
            DueDate = dueDate,
            DocumentCurrencyCode = documentCurrencyCode,
            TaxCurrencyCode = taxCurrencyCode,
            PaymentStatus = paymentStatus,
            EntryStatus = entryStatus,
            SyncDate = syncDate,
            SupplierPartyName = supplierPartyName,
            SupplierTIN = supplierTIN,
            SupplierBRN = supplierBRN,
            SupplierEmail = supplierEmail,
            SupplierTelephone = supplierTelephone,
            SupplierAddress = supplierAddress,
            CustomerPartyName = customerPartyName,
            CustomerTIN = customerTIN,
            CustomerBRN = customerBRN,
            CustomerEmail = customerEmail,
            CustomerTelephone = customerTelephone,
            CustomerAddress = customerAddress,
            LineExtensionAmount = lineExtensionAmount,
            TaxExclusiveAmount = taxExclusiveAmount,
            TaxInclusiveAmount = taxInclusiveAmount,
            TotalTaxAmount = totalTaxAmount,
            PayableAmount = payableAmount,
            PaidAmount = paidAmount,
            PayableRoundingAmount = payableRoundingAmount,
            Note = note,
            BuyerReference = buyerReference,
            AccountingCost = accountingCost,
            InvoiceLinesJson = invoiceLinesJson,
            TaxTotalJson = taxTotalJson,
            BusinessId = businessId,
            FirsBusinessId = firsBusinessId,
            IsReconciled = false,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Business Methods

    /// <summary>
    /// Updates the invoice data from a fresh sync (legacy - limited fields)
    /// </summary>
    public void UpdateFromSync(
        string paymentStatus,
        string entryStatus,
        decimal? paidAmount,
        string? syncDate,
        Guid updatedBy)
    {
        PaymentStatus = paymentStatus;
        EntryStatus = entryStatus;
        PaidAmount = paidAmount;
        SyncDate = syncDate;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates all invoice data from a fresh sync with complete invoice information
    /// </summary>
    public void UpdateAllFromSync(
        string invoiceTypeCode,
        DateOnly issueDate,
        string documentCurrencyCode,
        string taxCurrencyCode,
        string paymentStatus,
        string entryStatus,
        string supplierPartyName,
        TIN supplierTIN,
        string customerPartyName,
        TIN customerTIN,
        decimal lineExtensionAmount,
        decimal taxExclusiveAmount,
        decimal taxInclusiveAmount,
        decimal totalTaxAmount,
        decimal payableAmount,
        Guid updatedBy,
        string? issueTime = null,
        DateOnly? dueDate = null,
        string? syncDate = null,
        string? supplierBRN = null,
        string? supplierEmail = null,
        string? supplierTelephone = null,
        Address? supplierAddress = null,
        string? customerBRN = null,
        string? customerEmail = null,
        string? customerTelephone = null,
        Address? customerAddress = null,
        decimal? paidAmount = null,
        decimal? payableRoundingAmount = null,
        string? note = null,
        string? buyerReference = null,
        string? accountingCost = null,
        string? invoiceLinesJson = null,
        string? taxTotalJson = null)
    {
        // Update all fields with new values from sync
        InvoiceTypeCode = invoiceTypeCode;
        IssueDate = issueDate;
        IssueTime = issueTime;
        DueDate = dueDate;
        DocumentCurrencyCode = documentCurrencyCode;
        TaxCurrencyCode = taxCurrencyCode;
        PaymentStatus = paymentStatus;
        EntryStatus = entryStatus;
        SyncDate = syncDate;

        SupplierPartyName = supplierPartyName;
        SupplierTIN = supplierTIN;
        SupplierBRN = supplierBRN;
        SupplierEmail = supplierEmail;
        SupplierTelephone = supplierTelephone;
        SupplierAddress = supplierAddress;

        CustomerPartyName = customerPartyName;
        CustomerTIN = customerTIN;
        CustomerBRN = customerBRN;
        CustomerEmail = customerEmail;
        CustomerTelephone = customerTelephone;
        CustomerAddress = customerAddress;

        LineExtensionAmount = lineExtensionAmount;
        TaxExclusiveAmount = taxExclusiveAmount;
        TaxInclusiveAmount = taxInclusiveAmount;
        TotalTaxAmount = totalTaxAmount;
        PayableAmount = payableAmount;
        PaidAmount = paidAmount;
        PayableRoundingAmount = payableRoundingAmount;

        Note = note;
        BuyerReference = buyerReference;
        AccountingCost = accountingCost;
        InvoiceLinesJson = invoiceLinesJson;
        TaxTotalJson = taxTotalJson;

        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignToInputVatSchedule(Guid scheduleId)
    {
        InputVatScheduleId = scheduleId;
    }

    public void AssignToWhtSchedule(Guid scheduleId)
    {
        WhtScheduleId = scheduleId;
    }

    /// <summary>
    /// Associates this invoice with a business
    /// </summary>
    public void AssociateBusiness(Guid businessId, Guid updatedBy)
    {
        if (businessId == Guid.Empty)
            throw new BadRequestException("Business ID cannot be empty", nameof(businessId));

        BusinessId = businessId;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the invoice as reconciled
    /// </summary>
    public void MarkAsReconciled(Guid reconciledBy)
    {
        if (IsReconciled)
            throw new InvalidOperationException("Invoice is already reconciled");

        IsReconciled = true;
        ReconciledAt = DateTimeOffset.UtcNow;
        ReconciledBy = reconciledBy;
        UpdatedBy = reconciledBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Unmarks the invoice as reconciled
    /// </summary>
    public void UnmarkReconciliation(Guid updatedBy)
    {
        if (!IsReconciled)
            throw new InvalidOperationException("Invoice is not reconciled");

        IsReconciled = false;
        ReconciledAt = null;
        ReconciledBy = null;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates payment information from sync
    /// </summary>
    public void UpdatePaymentInfo(string paymentStatus, decimal? paidAmount, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(paymentStatus))
            throw new BadRequestException("Payment status cannot be empty", nameof(paymentStatus));

        PaymentStatus = paymentStatus;
        PaidAmount = paidAmount;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates payment status with optional reference (for manual buyer action)
    /// </summary>
    public void UpdatePaymentStatus(string paymentStatus, string? paymentReference = null)
    {
        if (string.IsNullOrWhiteSpace(paymentStatus))
            throw new BadRequestException("Payment status cannot be empty", nameof(paymentStatus));

        PaymentStatus = paymentStatus;
        if (!string.IsNullOrWhiteSpace(paymentReference))
            PaymentReference = paymentReference;
    }

    #endregion

    #region Validation

    private static void ValidateInputs(
        IRN irn,
        TIN supplierTIN,
        TIN customerTIN,
        string invoiceTypeCode,
        string documentCurrencyCode,
        string supplierPartyName,
        string customerPartyName)
    {
        if (irn == null)
            throw new BadRequestException("IRN cannot be null", nameof(irn));

        if (supplierTIN == null)
            throw new BadRequestException("Supplier TIN cannot be null", nameof(supplierTIN));

        if (customerTIN == null)
            throw new BadRequestException("Customer TIN cannot be null", nameof(customerTIN));

        if (string.IsNullOrWhiteSpace(invoiceTypeCode))
            throw new BadRequestException("Invoice type code is required", nameof(invoiceTypeCode));

        if (string.IsNullOrWhiteSpace(documentCurrencyCode))
            throw new BadRequestException("Document currency code is required", nameof(documentCurrencyCode));

        if (string.IsNullOrWhiteSpace(supplierPartyName))
            throw new BadRequestException("Supplier party name is required", nameof(supplierPartyName));

        if (string.IsNullOrWhiteSpace(customerPartyName))
            throw new BadRequestException("Customer party name is required", nameof(customerPartyName));
    }

    #endregion
}
