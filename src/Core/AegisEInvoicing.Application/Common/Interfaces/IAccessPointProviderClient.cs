using AegisEInvoicing.Domain.Entities.InvoiceManagement;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Result returned by common APP provider operations.
/// </summary>
public sealed record AppProviderResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse)
{
    public static AppProviderResult Success(string? rawResponse = null)
        => new(true, null, null, rawResponse);

    public static AppProviderResult Failure(string errorCode, string errorMessage, string? rawResponse = null)
        => new(false, errorCode, errorMessage, rawResponse);
}

/// <summary>
/// Result of a TIN lookup operation.
/// </summary>
public sealed record AppLookupTinResult(
    bool IsSuccess,
    bool IsUp,
    string? BusinessReference,
    string? ErrorMessage);

/// <summary>
/// Result of invoice signing operation.
/// </summary>
public sealed record AppSignInvoiceResult(
    bool IsSuccess,
    string? Irn,
    DateTime? SignedDate,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse);

/// <summary>
/// Result of invoice validation operation.
/// </summary>
public sealed record AppValidateInvoiceResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse);

/// <summary>
/// Result of IRN validation operation.
/// </summary>
public sealed record AppValidateIRNResult(
    bool IsSuccess,
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse);

/// <summary>
/// Result of invoice confirmation operation.
/// </summary>
public sealed record AppConfirmInvoiceResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse);

/// <summary>
/// Result of payment status update operation.
/// </summary>
public sealed record AppUpdateStatusResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse);

/// <summary>
/// Result of invoice download operation.
/// </summary>
public sealed record AppDownloadInvoiceResult(
    bool IsSuccess,
    string? EncryptedData,
    string? Iv,
    string? PublicKey,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse);

/// <summary>
/// Result of invoice search operation.
/// </summary>
public sealed record AppSearchInvoiceResult(
    bool IsSuccess,
    List<AppInvoiceSearchItem>? Items,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// Result of entity lookup operation.
/// </summary>
public sealed record AppGetEntityResult(
    bool IsSuccess,
    object? EntityData,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// Result of IRN lookup operation.
/// </summary>
public sealed record AppLookupWithIRNResult(
    bool IsSuccess,
    AppPartyInfo? SupplierParty,
    AppPartyInfo? CustomerParty,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// Result of GetPurchaseInvoices operation.
/// </summary>
public sealed record AppGetPurchaseInvoicesResult(
    bool IsSuccess,
    int Count,
    List<AppPurchaseInvoiceItem>? Items,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// Party information (supplier or customer).
/// </summary>
public sealed record AppPartyInfo(
    string? Id,
    string PartyName,
    string TIN,
    string? BRN,
    string? BusinessDescription,
    string? Email,
    string? Telephone,
    string? PostalAddressId,
    AppAddress? Address);

/// <summary>
/// Address information.
/// </summary>
public sealed record AppAddress(
    string? StreetName,
    string? AdditionalStreetName,
    string? CityName,
    string? PostalZone,
    string? CountrySubentity,
    string? CountryIdentificationCode);

/// <summary>
/// Legal monetary total information.
/// </summary>
public sealed record AppMonetaryTotal(
    decimal LineExtensionAmount,
    decimal TaxExclusiveAmount,
    decimal TaxInclusiveAmount,
    decimal TotalTaxAmount,
    decimal PayableAmount,
    decimal? PaidAmount,
    decimal? PayableRoundingAmount);

/// <summary>
/// Invoice line item.
/// </summary>
public sealed record AppInvoiceLine(
    int InvoicedQuantity,
    decimal LineExtensionAmount,
    string ItemName,
    string ItemDescription,
    decimal PriceAmount,
    int BaseQuantity,
    string PriceUnit,
    string HsnCode,
    string ProductCategory,
    string IsicCode,
    string ServiceCategory,
    decimal DiscountRate,
    decimal DiscountAmount,
    decimal FeeRate,
    decimal FeeAmount);

/// <summary>
/// Tax total breakdown.
/// </summary>
public sealed record AppTaxTotal(
    string TaxSchemeId,
    decimal TaxableAmount,
    decimal TaxAmount,
    string? TaxCategoryId = null,
    decimal? Percent = null);

/// <summary>
/// Invoice delivery period.
/// </summary>
public sealed record AppDeliveryPeriod(
    DateTime? StartDate,
    DateTime? EndDate);

/// <summary>
/// Payment means information.
/// </summary>
public sealed record AppPaymentMeans(
    string PaymentMeansCode,
    string? PaymentId,
    string? InstructionNote);

/// <summary>
/// Individual purchase invoice item.
/// </summary>
public sealed record AppPurchaseInvoiceItem(
    string? BusinessId,
    string IRN,
    string? CustomizationId,
    string InvoiceTypeCode,
    DateTime IssueDate,
    string IssueTime,
    DateTime? DueDate,
    string DocumentCurrencyCode,
    string TaxCurrencyCode,
    string PaymentStatus,
    string EntryStatus,
    string? SyncDate,
    AppPartyInfo? SupplierParty,
    AppPartyInfo? CustomerParty,
    AppMonetaryTotal? LegalMonetaryTotal,
    List<AppTaxTotal>? TaxTotal,
    List<AppInvoiceLine>? InvoiceLines,
    string? Note,
    string? BuyerReference,
    string? AccountingCost,
    AppDeliveryPeriod? InvoiceDeliveryPeriod,
    List<AppPaymentMeans>? PaymentMeans,
    string? PaymentTermsNote);

/// <summary>
/// Invoice search item.
/// </summary>
public sealed record AppInvoiceSearchItem(
    string IRN,
    string InvoiceTypeCode,
    DateTime IssueDate,
    string PaymentStatus,
    decimal PayableAmount);

/// <summary>
/// Vendor-agnostic interface for Access Point Provider (APP) operations.
/// Implemented by each provider adapter (Interswitch, BlueBridge, eTranzact, …).
/// Adapters self-register via DI; the router discovers them by ProviderCode — no enum needed.
/// </summary>
public interface IAccessPointProviderClient
{
    /// <summary>
    /// Unique, lowercase, stable key that identifies this adapter (e.g. "interswitch").
    /// Must match the AdapterKey stored in AppProviderConfiguration and Business.
    /// </summary>
    string ProviderCode { get; }

    /// <summary>
    /// Human-readable display name shown in the admin UI (e.g. "Interswitch").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Configure this adapter with decrypted runtime credentials from AppProviderConfiguration.
    /// Called by AppProviderRouter before the adapter is returned to callers.
    /// Each adapter owns its own credential JSON schema — the router treats the blob as opaque.
    /// </summary>
    void Configure(string baseUrl, string? credentialsJson);

    /// <summary>
    /// Whether this provider supports TIN lookup before transmission.
    /// True for Interswitch; false for BlueBridge and eTranzact.
    /// </summary>
    bool SupportsLookupTin { get; }

    /// <summary>
    /// Transmit a signed invoice (by IRN) to the NRS portal via this provider.
    /// </summary>
    Task<AppProviderResult> TransmitAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup a business by TIN to verify enrollment on the NRS portal.
    /// Returns a failure result if <see cref="SupportsLookupTin"/> is false.
    /// </summary>
    Task<AppLookupTinResult> LookupTinAsync(string tin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Health-check ping to verify the provider's API is reachable.
    /// </summary>
    Task<(bool IsHealthy, string? Message)> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign an invoice with the NRS portal. The invoice entity should include all related navigation properties
    /// (Business, Party, InvoiceLine, etc.). Each adapter handles mapping to its provider-specific request format.
    /// </summary>
    Task<AppSignInvoiceResult> SignInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an invoice before signing. The invoice entity should include all related navigation properties.
    /// </summary>
    Task<AppValidateInvoiceResult> ValidateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an IRN with the NRS portal.
    /// </summary>
    Task<AppValidateIRNResult> ValidateIRNAsync(string irn, string invoiceReference, string businessId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm an invoice with the NRS portal (mark as received/accepted).
    /// </summary>
    Task<AppConfirmInvoiceResult> ConfirmInvoiceAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update payment status of an invoice on the NRS portal.
    /// </summary>
    Task<AppUpdateStatusResult> UpdateStatusAsync(string irn, string paymentStatus, string? reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all purchase invoices (received invoices) for a business within a date range.
    /// </summary>
    Task<AppGetPurchaseInvoicesResult> GetPurchaseInvoicesAsync(string tin, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup invoice details by IRN.
    /// </summary>
    Task<AppLookupWithIRNResult> LookupWithIRNAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download encrypted invoice data from the NRS portal.
    /// </summary>
    Task<AppDownloadInvoiceResult> DownloadInvoiceAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for invoices by business ID.
    /// </summary>
    Task<AppSearchInvoiceResult> SearchInvoiceAsync(string businessId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entity information by entity ID (e.g., business details).
    /// </summary>
    Task<AppGetEntityResult> GetEntityAsync(string entityId, CancellationToken cancellationToken = default);
}

