namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;

/// <summary>
/// DTO for returning received invoice data to API consumers
/// </summary>
public sealed class ReceivedInvoiceDto
{
    public Guid Id { get; set; }
    public string IRN { get; set; } = string.Empty;
    public string SenderBusinessName { get; set; } = string.Empty;
    public string InvoiceTypeCode { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public string? IssueTime { get; set; }
    public DateOnly? DueDate { get; set; }

    // Currency Information
    public string DocumentCurrencyCode { get; set; } = string.Empty;
    public string TaxCurrencyCode { get; set; } = string.Empty;

    // Status Information
    public string PaymentStatus { get; set; } = string.Empty;
    public string EntryStatus { get; set; } = string.Empty;
    public string? SyncDate { get; set; }

    // Supplier Information
    public string SupplierPartyName { get; set; } = string.Empty;
    public string SupplierTIN { get; set; } = string.Empty;
    public string? SupplierBRN { get; set; }
    public string? SupplierEmail { get; set; }
    public string? SupplierTelephone { get; set; }
    public AddressDto? SupplierAddress { get; set; }

    // Customer Information
    public string CustomerPartyName { get; set; } = string.Empty;
    public string CustomerTIN { get; set; } = string.Empty;
    public string? CustomerBRN { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerTelephone { get; set; }
    public AddressDto? CustomerAddress { get; set; }

    // Financial Amounts
    public decimal LineExtensionAmount { get; set; }
    public decimal TaxExclusiveAmount { get; set; }
    public decimal TaxInclusiveAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? PayableRoundingAmount { get; set; }

    // Additional Information
    public string? Note { get; set; }
    public string? BuyerReference { get; set; }
    public string? AccountingCost { get; set; }

    // Business Association
    public Guid? BusinessId { get; set; }
    public string? BusinessName { get; set; }

    // Reconciliation Status
    public bool IsReconciled { get; set; }
    public DateTimeOffset? ReconciledAt { get; set; }
    public Guid? ReconciledBy { get; set; }

    // Audit Fields
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Address DTO for received invoices
/// </summary>
public sealed class AddressDto
{
    public string StreetName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string CountrySubentity { get; set; } = string.Empty;
    public string CountryIdentificationCode { get; set; } = string.Empty;
    public string? PostalZone { get; set; }
    public string? AdditionalStreetName { get; set; }
    public string? Lga { get; set; }
    public string State => CountrySubentity;
}
