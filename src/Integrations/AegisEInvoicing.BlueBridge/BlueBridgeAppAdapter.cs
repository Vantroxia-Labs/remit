using System.Text.Json;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.BlueBridge.Contracts;
using AegisEInvoicing.BlueBridge.Models.Requests;
using AegisEInvoicing.BlueBridge.Converters;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.BlueBridge;

/// <summary>
/// Adapts <see cref="IBlueBridgeClient"/> to the vendor-agnostic
/// <see cref="IAccessPointProviderClient"/> interface used by <see cref="AppProviderRouter"/>.
///
/// Credential JSON schema:
/// { "apiKey": "..." }
/// </summary>
public sealed class BlueBridgeAppAdapter(
    IBlueBridgeClient client,
    ILogger<BlueBridgeAppAdapter> logger) : IAccessPointProviderClient
{
    /// <inheritdoc />
    public string ProviderCode => "bluebridge";

    /// <inheritdoc />
    public string DisplayName => "BlueBridge";

    /// <inheritdoc />
    public bool SupportsLookupTin => false;

    /// <inheritdoc />
    public void Configure(string baseUrl, string? credentialsJson)
    {
        string apiKey = string.Empty;

        if (credentialsJson is not null)
        {
            using var doc = JsonDocument.Parse(credentialsJson);
            var root = doc.RootElement;
            apiKey = root.TryGetProperty("apiKey", out var k) ? k.GetString() ?? string.Empty : string.Empty;
        }

        client.Configure(baseUrl, apiKey);

        logger.LogInformation(
            "BlueBridge adapter configured with base URL: {BaseUrl}, ApiKey present: {HasKey}",
            baseUrl, !string.IsNullOrWhiteSpace(apiKey));
    }

    /// <inheritdoc />
    public async Task<AppProviderResult> TransmitAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.TransmitInvoiceAsync(irn, cancellationToken);

            return response.IsSuccess
                ? AppProviderResult.Success(rawResponse: JsonSerializer.Serialize(response))
                : AppProviderResult.Failure(
                    errorCode: "BLUEBRIDGE_TRANSMIT_ERROR",
                    errorMessage: response.Message ?? "Transmission failed",
                    rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge TransmitInvoice failed for IRN {Irn}", irn);
            return AppProviderResult.Failure("BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>BlueBridge does not support TIN enrollment lookup.</remarks>
    public Task<AppLookupTinResult> LookupTinAsync(
        string tin,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AppLookupTinResult(false, false, null, "BlueBridge does not support TIN lookup."));

    /// <inheritdoc />
    public async Task<(bool IsHealthy, string? Message)> PingAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthy = await client.TestConnectionAsync(cancellationToken);
            return healthy
                ? (true, null)
                : (false, "BlueBridge API did not respond successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge PingAsync failed");
            return (false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppSignInvoiceResult> SignInvoiceAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Map domain Invoice to BlueBridgeInvoiceRequest
            var request = MapInvoiceToBlueBridgeRequest(invoice);
            var response = await client.SignInvoiceAsync(request, cancellationToken);

            if (response.IsSuccess)
            {
                return new AppSignInvoiceResult(
                    IsSuccess: true,
                    Irn: invoice.Irn?.Value,
                    SignedDate: DateTime.UtcNow,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            return new AppSignInvoiceResult(
                IsSuccess: false,
                Irn: null,
                SignedDate: null,
                ErrorCode: "BLUEBRIDGE_SIGN_FAILED",
                ErrorMessage: response.Message ?? "Invoice signing failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge SignInvoice failed for invoice {InvoiceId}", invoice.Id);
            return new AppSignInvoiceResult(false, null, null, "BLUEBRIDGE_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppValidateInvoiceResult> ValidateInvoiceAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = MapInvoiceToBlueBridgeRequest(invoice);
            var response = await client.ValidateInvoiceAsync(request, cancellationToken);

            if (response.IsSuccess)
            {
                return new AppValidateInvoiceResult(
                    IsSuccess: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            return new AppValidateInvoiceResult(
                IsSuccess: false,
                ErrorCode: "BLUEBRIDGE_VALIDATE_FAILED",
                ErrorMessage: response.Message ?? "Invoice validation failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge ValidateInvoice failed for invoice {InvoiceId}", invoice.Id);
            return new AppValidateInvoiceResult(false, "BLUEBRIDGE_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppValidateIRNResult> ValidateIRNAsync(
        string irn,
        string invoiceReference,
        string businessId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ValidateIrnRequest
            {
                Irn = irn,
                InvoiceReference = invoiceReference,
                BusinessId = businessId
            };

            var response = await client.ValidateIrnAsync(request, cancellationToken);

            if (response.IsSuccess)
            {
                return new AppValidateIRNResult(
                    IsSuccess: true,
                    IsValid: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            return new AppValidateIRNResult(
                IsSuccess: false,
                IsValid: false,
                ErrorCode: "BLUEBRIDGE_VALIDATE_IRN_FAILED",
                ErrorMessage: response.Status ?? "IRN validation failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge ValidateIRN failed for IRN {Irn}", irn);
            return new AppValidateIRNResult(false, false, "BLUEBRIDGE_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppConfirmInvoiceResult> ConfirmInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.ConfirmInvoiceAsync(irn, cancellationToken);

            if (response.IsSuccess)
            {
                return new AppConfirmInvoiceResult(
                    IsSuccess: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            return new AppConfirmInvoiceResult(
                IsSuccess: false,
                ErrorCode: "BLUEBRIDGE_CONFIRM_FAILED",
                ErrorMessage: response.Message ?? "Invoice confirmation failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge ConfirmInvoice failed for IRN {Irn}", irn);
            return new AppConfirmInvoiceResult(false, "BLUEBRIDGE_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppUpdateStatusResult> UpdateStatusAsync(
        string irn,
        string paymentStatus,
        string? reference,
        decimal? amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdateInvoiceRequest
            {
                PaymentStatus = paymentStatus,
                Reference = reference,
                Amount = amount
            };

            var response = await client.UpdateInvoiceAsync(irn, request, cancellationToken);

            if (response.IsSuccess)
            {
                return new AppUpdateStatusResult(
                    IsSuccess: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            return new AppUpdateStatusResult(
                IsSuccess: false,
                ErrorCode: "BLUEBRIDGE_UPDATE_FAILED",
                ErrorMessage: response.Message ?? "Status update failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge UpdateStatus failed for IRN {Irn}", irn);
            return new AppUpdateStatusResult(false, "BLUEBRIDGE_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public Task<AppGetPurchaseInvoicesResult> GetPurchaseInvoicesAsync(
        string tin,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        // BlueBridge does not support fetching purchase invoices
        logger.LogWarning("BlueBridge GetPurchaseInvoices called but not supported");
        return Task.FromResult(new AppGetPurchaseInvoicesResult(
            IsSuccess: false,
            Count: 0,
            Items: null,
            ErrorCode: "BLUEBRIDGE_NOT_SUPPORTED",
            ErrorMessage: "BlueBridge does not support GetPurchaseInvoices operation"));
    }

    /// <inheritdoc />
    public async Task<AppLookupWithIRNResult> LookupWithIRNAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.LookupWithIrnAsync(irn, cancellationToken);

            if (response.IsSuccess)
            {
                // Map response to AppLookupWithIRNResult
                // TODO: Map actual party data from response when needed
                return new AppLookupWithIRNResult(
                    IsSuccess: true,
                    SupplierParty: null,
                    CustomerParty: null,
                    ErrorCode: null,
                    ErrorMessage: null);
            }

            return new AppLookupWithIRNResult(
                IsSuccess: false,
                SupplierParty: null,
                CustomerParty: null,
                ErrorCode: "BLUEBRIDGE_LOOKUP_FAILED",
                ErrorMessage: response.Message ?? "Lookup with IRN failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge LookupWithIRN failed for IRN {Irn}", irn);
            return new AppLookupWithIRNResult(false, null, null, "BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<AppDownloadInvoiceResult> DownloadInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        // BlueBridge does not support invoice download
        logger.LogWarning("BlueBridge DownloadInvoice called but not supported");
        return Task.FromResult(new AppDownloadInvoiceResult(
            IsSuccess: false,
            EncryptedData: null,
            Iv: null,
            PublicKey: null,
            ErrorCode: "BLUEBRIDGE_NOT_SUPPORTED",
            ErrorMessage: "BlueBridge does not support DownloadInvoice operation",
            RawResponse: null));
    }

    /// <inheritdoc />
    public async Task<AppSearchInvoiceResult> SearchInvoiceAsync(
        string businessId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.SearchInvoicesAsync(businessId, cancellationToken);

            if (response.IsSuccess)
            {
                // Map response to AppSearchInvoiceResult
                // TODO: Map actual invoice items when needed
                return new AppSearchInvoiceResult(
                    IsSuccess: true,
                    Items: new List<AppInvoiceSearchItem>(),
                    ErrorCode: null,
                    ErrorMessage: null);
            }

            return new AppSearchInvoiceResult(
                IsSuccess: false,
                Items: null,
                ErrorCode: "BLUEBRIDGE_SEARCH_FAILED",
                ErrorMessage: response.Message ?? "Search invoice failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge SearchInvoice failed for business {BusinessId}", businessId);
            return new AppSearchInvoiceResult(false, null, "BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<AppGetEntityResult> GetEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        // BlueBridge does not support entity lookup
        logger.LogWarning("BlueBridge GetEntity called but not supported");
        return Task.FromResult(new AppGetEntityResult(
            IsSuccess: false,
            EntityData: null,
            ErrorCode: "BLUEBRIDGE_NOT_SUPPORTED",
            ErrorMessage: "BlueBridge does not support GetEntity operation"));
    }

    // Helper method to map domain Invoice to BlueBridgeInvoiceRequest
    private static BlueBridgeInvoiceRequest MapInvoiceToBlueBridgeRequest(Invoice invoice)
    {
        return new BlueBridgeInvoiceRequest
        {
            BusinessId = invoice.Business.FIRSBusinessId.ToString(),
            Irn = invoice.Irn?.Value ?? string.Empty,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            IssueTime = invoice.IssueTime,
            InvoiceTypeCode = invoice.InvoiceType.Code.ToString(),
            InvoiceKind = invoice.InvoiceKind?.ToString(),
            PaymentStatus = invoice.PaymentStatus.ToString(),
            Note = invoice.Note,
            DocumentCurrencyCode = invoice.Currency.Code,
            TaxCurrencyCode = invoice.Currency.Code,
            InvoiceDeliveryPeriod = invoice.DeliveryPeriod.ToBlueBridgeInvoiceDeliveryPeriod(),
            BillingReference = invoice.BillingReferences.ToList().ToBlueBridgeBillingReference(),
            DispatchDocumentReference = invoice.DispatchDocumentReference!.ToBlueBridgeDispatchDocumentReference(),
            ReceiptDocumentReference = invoice.ReceiptDocumentReference!.ToBlueBridgeReceiptDocumentReference(),
            OriginatorDocumentReference = invoice.OriginatorDocumentReference!.ToBlueBridgeOriginatorDocumentReference(),
            ContractDocumentReference = invoice.ContractDocumentReference!.ToBlueBridgeContractDocumentReference(),
            AdditionalDocumentReference = invoice.AdditionalDocumentReferences.ToList().ToBlueBridgeAddtionalDocumentReference(),
            AccountingCustomerParty = invoice.Party.ToBlueBridgeAccountingCustomerParty(),
            AccountingSupplierParty = invoice.Business.ToBlueBridgeAccountingSupplierParty(),
            PaymentMeans = invoice.PaymentMeans!.ToBlueBridgePaymentMeans(invoice.IssueDate.AddDays(7)),
            PaymentTermsNote = invoice.PaymentTerms,
            AllowanceCharge = invoice.InvoiceLine.ToList().ToBlueBridgeAllowanceCharge(),
            TaxTotal = invoice.InvoiceLine.ToList().ToBlueBridgeTaxTotal(),
            LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToBlueBridgeLegalMonetaryTotal(),
            InvoiceLine = invoice.InvoiceLine.ToList().ToBlueBridgeInvoiceLine(invoice.Currency.Code)
        };
    }
}
