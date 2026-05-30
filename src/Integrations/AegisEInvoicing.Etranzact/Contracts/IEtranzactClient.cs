using AegisEInvoicing.Etranzact.Models.Requests;
using AegisEInvoicing.Etranzact.Models.Responses;

namespace AegisEInvoicing.Etranzact.Contracts;

/// <summary>
/// HTTP client interface for eTranzact e-invoice integration.
/// Authentication uses per-request HMAC-SHA256 signed headers:
///   X-API-Key, X-API-Signature, X-API-Timestamp.
/// </summary>
public interface IEtranzactClient
{
    /// <summary>
    /// Validates the structure and content of an invoice prior to signing.
    /// POST /api/v1/app/invoice/validate
    /// </summary>
    Task<ValidateInvoiceResponse> ValidateInvoiceAsync(
        ValidateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Digitally signs a validated invoice.
    /// POST /api/v1/app/invoice/sign
    /// </summary>
    Task<SignInvoiceResponse> SignInvoiceAsync(
        SignInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transmits a signed invoice to NRS for official registration.
    /// POST /api/v1/app/invoice/transmit
    /// </summary>
    Task<TransmitInvoiceResponse> TransmitInvoiceAsync(
        TransmitInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a received invoice by IRN.
    /// GET /api/v1/app/invoice/confirm/{irn}
    /// </summary>
    Task<ConfirmInvoiceResponse> ConfirmInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the payment status of a transmitted invoice.
    /// PATCH /api/v1/app/invoice/update/{irn}
    /// </summary>
    Task<UpdatePaymentStatusResponse> UpdatePaymentStatusAsync(
        string irn,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a taxpayer TIN and retrieves taxpayer information.
    /// Equivalent to Interswitch's LookupWithTIN.
    /// POST /api/v1/resource/verify-tin
    /// </summary>
    Task<VerifyTinResponse> VerifyTinAsync(
        VerifyTinRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an Invoice Reference Number (IRN) and confirms it exists in NRS.
    /// Equivalent to Interswitch's LookupWithIRN.
    /// POST /api/v1/app/invoice/validate-irn
    /// </summary>
    Task<ValidateIrnResponse> ValidateIrnAsync(
        ValidateIrnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an invoice document. Not yet available on the eTranzact API.
    /// </summary>
    Task<NotImplementedException> DownloadInvoiceAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for invoices. Not yet available on the eTranzact API.
    /// </summary>
    Task<NotImplementedException> SearchInvoiceAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a taxpayer entity record. Not yet available on the eTranzact API.
    /// </summary>
    Task<NotImplementedException> GetEntityAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves purchase invoices. Not yet available on the eTranzact API.
    /// </summary>
    Task<NotImplementedException> GetPurchaseInvoicesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the eTranzact API.
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the client with credentials and base URL from the Access Point Provider (database).
    /// Overrides any values from appsettings/environment.
    /// </summary>
    void Configure(string baseUrl, string clientApiKey, string clientSecretKey);
}
