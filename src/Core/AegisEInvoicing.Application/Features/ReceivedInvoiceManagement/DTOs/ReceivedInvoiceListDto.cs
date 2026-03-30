namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;

/// <summary>
/// Lightweight DTO for listing received invoices (without detailed nested data)
/// </summary>
public sealed class ReceivedInvoiceListDto
{
    public Guid Id { get; set; }
    public string IRN { get; set; } = string.Empty;
    public string InvoiceTypeCode { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }

    // Currency Information
    public string DocumentCurrencyCode { get; set; } = string.Empty;

    // Status Information
    public string PaymentStatus { get; set; } = string.Empty;
    public string EntryStatus { get; set; } = string.Empty;

    // Supplier Information (minimal)
    public string SupplierPartyName { get; set; } = string.Empty;
    public string SupplierTIN { get; set; } = string.Empty;

    // Customer Information (minimal)
    public string CustomerPartyName { get; set; } = string.Empty;
    public string CustomerTIN { get; set; } = string.Empty;

    // Financial Amounts (key totals only)
    public decimal TaxInclusiveAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal? PaidAmount { get; set; }

    // Business Association
    public Guid? BusinessId { get; set; }
    public string? BusinessName { get; set; }

    // Reconciliation Status
    public bool IsReconciled { get; set; }
    public DateTimeOffset? ReconciledAt { get; set; }

    // Audit Fields
    public DateTimeOffset CreatedAt { get; set; }
}
