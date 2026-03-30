using AegisEInvoicing.Interswitch.Models.Requests.ConfirmInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.DownloadInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.GetEntity;
using AegisEInvoicing.Interswitch.Models.Requests.GetPurchaseInvoices;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithIRN;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithTIN;
using AegisEInvoicing.Interswitch.Models.Requests.SearchInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.SignInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.TransmitInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.UpdateStatus;
using AegisEInvoicing.Interswitch.Models.Requests.ValidateInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.ValidateIRN;
using AegisEInvoicing.Interswitch.Models.Responses.ConfirmInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.DownloadInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.GetEntity;
using AegisEInvoicing.Interswitch.Models.Responses.GetPurchaseInvoices;
using AegisEInvoicing.Interswitch.Models.Responses.LookupWithIRN;
using AegisEInvoicing.Interswitch.Models.Responses.LookupWithTIN;
using AegisEInvoicing.Interswitch.Models.Responses.SearchInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.SignInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.TransmitInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.UpdateStatus;
using AegisEInvoicing.Interswitch.Models.Responses.ValidateInvoice;
using AegisEInvoicing.Interswitch.Models.Responses.ValidateIRN;

namespace AegisEInvoicing.Interswitch.Interfaces;

/// <summary>
/// HTTP client interface for Interswitch SwitchTax integration.
/// This service provides invoice validation, signing, and transmission to FIRS via Interswitch.
/// </summary>
public interface IInterswitchHttpClient
{
    /// <summary>
    /// Validates an invoice by IRN to confirm it exists, has been properly issued, and is valid in the FIRS system
    /// </summary>
    /// <param name="request">Validation request with IRN, invoice reference, and business ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation response indicating if IRN is valid</returns>
    Task<ValidateIRNResponse> ValidateIRNAsync(
        ValidateIRNRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the structure and content of an invoice prior to signing and transmission
    /// </summary>
    /// <param name="request">Invoice payload to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation response indicating if invoice structure is valid</returns>
    Task<ValidateInvoiceResponse> ValidateInvoiceAsync(
        ValidateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Digitally signs a validated invoice with FIRS-approved credentials before transmission
    /// </summary>
    /// <param name="request">Invoice payload to sign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signing response with signature details</returns>
    Task<SignInvoiceResponse> SignInvoiceAsync(
        SignInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Digitally confirms a recieved invoice
    /// </summary>
    /// <param name="request">Invoice payload to confirm</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>confirmation response with details</returns>
    Task<ConfirmInvoiceWrappedResponse> ConfirmInvoiceAsync(
        ConfirmInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the payment status of a previously transmitted invoice (e.g. when payment is completed)
    /// </summary>
    /// <param name="request">Status update request with payment status and IRN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update response confirming status change</returns>
    Task<UpdateStatusResponse> UpdateStatusAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the encrypted version of an invoice and returns its decrypted content
    /// </summary>
    /// <param name="request">Download request with IRN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encrypted invoice data with IV and public key</returns>
    Task<DownloadInvoiceResponse> DownloadInvoiceAsync(
        DownloadInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for a specific invoice within the registered business by IRN
    /// </summary>
    /// <param name="request">Search request with IRN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results with invoice details</returns>
    Task<SearchInvoiceResponse> SearchInvoiceAsync(
        SearchInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup business party information using Invoice Reference Number
    /// </summary>
    /// <param name="request">Lookup request with IRN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier and customer party information</returns>
    Task<LookupWithIRNResponse> LookupWithIRNAsync(
        LookupWithIRNRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transmits a signed invoice to FIRS for official registration and IRN confirmation
    /// </summary>
    /// <param name="request">Transmission request with IRN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transmission response confirming successful submission</returns>
    Task<TransmitInvoiceResponse> TransmitInvoiceAsync(
        TransmitInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches business registration and tax profile information using a TIN
    /// </summary>
    /// <param name="request">Lookup request with TIN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supplier and customer party information</returns>
    Task<LookupWithTINResponse> LookupWithTINAsync(
        LookupWithTINRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches complete taxpayer entity information including all registered businesses
    /// </summary>
    /// <param name="request">Entity request with EntityId</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete entity and business profile data</returns>
    Task<GetEntityResponse> GetEntityAsync(
        GetEntityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches purchase invoices (received invoices) based on taxpayer TIN and date ranges
    /// </summary>
    /// <param name="request">Purchase invoices request with TIN and date range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of purchase invoices with pagination information</returns>
    Task<GetPurchaseInvoicesResponse> GetPurchaseInvoicesAsync(
        GetPurchaseInvoicesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the Interswitch API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
