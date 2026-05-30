using AegisEInvoicing.BlueBridge.Models.Requests;
using AegisEInvoicing.BlueBridge.Models.Responses;

namespace AegisEInvoicing.BlueBridge.Contracts;

/// <summary>
/// HTTP client interface for BlueBridge e-invoice integration.
/// All requests are authenticated via the <c>X-API-Key</c> header.
/// Base URL: https://blugateway.bluechiptech.biz
/// </summary>
public interface IBlueBridgeClient
{
    /// <summary>
    /// Configures the client with credentials from the Access Point Provider.
    /// </summary>
    void Configure(string baseUrl, string apiKey);

    /// <summary>
    /// Generates a unique Invoice Reference Number (IRN) for a given reference.
    /// GET /api/v1/invoices/generate-irn?reference={reference}
    /// </summary>
    Task<GenerateIrnResponse> GenerateIrnAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the IRN against FIRS standard format.
    /// POST /api/v1/invoices/validate-irn
    /// </summary>
    Task<ValidateIrnResponse> ValidateIrnAsync(ValidateIrnRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates full invoice data against FIRS rules before signing.
    /// POST /api/v1/invoices/validate
    /// </summary>
    Task<ValidateInvoiceResponse> ValidateInvoiceAsync(BlueBridgeInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs and transmits the invoice to the FIRS system.
    /// POST /api/v1/invoices/sign
    /// </summary>
    Task<SignInvoiceResponse> SignInvoiceAsync(BlueBridgeInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transmits a signed invoice to FIRS for official recording.
    /// POST /api/v1/invoices/transmit/:irn
    /// </summary>
    Task<TransmitInvoiceResponse> TransmitInvoiceAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves invoices associated with a Taxpayer Identification Number.
    /// GET /api/v1/invoices/transmit/lookup/tin/:tin
    /// </summary>
    Task<LookupWithTinResponse> LookupWithTinAsync(string tin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific transmitted invoice record by IRN.
    /// GET /api/v1/invoices/lookup/:irn
    /// </summary>
    Task<LookupWithIrnResponse> LookupWithIrnAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the payment status or reference of an invoice.
    /// PATCH /api/v1/invoices/update/:irn
    /// </summary>
    Task<UpdateInvoiceResponse> UpdateInvoiceAsync(string irn, UpdateInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches invoices associated with a business ID.
    /// GET /api/v1/invoices/:businessId
    /// </summary>
    Task<SearchInvoicesResponse> SearchInvoicesAsync(string businessId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the health status of the BlueBridge invoice service.
    /// GET /api/v1/invoices/health
    /// </summary>
    Task<HealthCheckResponse> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms an invoice by IRN.
    /// GET /api/v1/invoices/confirm/:irn
    /// </summary>
    Task<ConfirmInvoiceResponse> ConfirmInvoiceAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks connectivity to the BlueBridge API.
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
