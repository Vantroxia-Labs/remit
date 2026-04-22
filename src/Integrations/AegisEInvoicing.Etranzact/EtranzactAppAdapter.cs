using System.Text.Json;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Etranzact.Contracts;
using AegisEInvoicing.Etranzact.Models.Requests;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Etranzact;

/// <summary>
/// Adapts <see cref="IEtranzactClient"/> to the vendor-agnostic
/// <see cref="IAccessPointProviderClient"/>
///
/// Credential JSON schema:
/// { "clientApiKey": "...", "clientSecretKey": "..." }
/// </summary>
public sealed class EtranzactAppAdapter(
    IEtranzactClient client,
    ILogger<EtranzactAppAdapter> logger) : IAccessPointProviderClient
{
    /// <inheritdoc />
    public string ProviderCode => "etranzact";

    /// <inheritdoc />
    public string DisplayName => "eTranzact";

    /// <inheritdoc />
    public bool SupportsLookupTin => true;

    /// <inheritdoc />
    public void Configure(string baseUrl, string? credentialsJson)
    {
        string clientApiKey = string.Empty, clientSecretKey = string.Empty;

        if (credentialsJson is not null)
        {
            using var doc = JsonDocument.Parse(credentialsJson);
            var root = doc.RootElement;
            clientApiKey = root.TryGetProperty("clientApiKey", out var k) ? k.GetString() ?? string.Empty : string.Empty;
            clientSecretKey = root.TryGetProperty("clientSecretKey", out var s) ? s.GetString() ?? string.Empty : string.Empty;
        }

        client.Configure(baseUrl, clientApiKey, clientSecretKey);

        logger.LogInformation(
            "eTranzact adapter configured with base URL: {BaseUrl}, ApiKey present: {HasKey}",
            baseUrl, !string.IsNullOrWhiteSpace(clientApiKey));
    }

    /// <inheritdoc />
    public async Task<AppProviderResult> TransmitAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.TransmitInvoiceAsync(
                new TransmitInvoiceRequest { Irn = irn }, cancellationToken);

            return response.IsSuccess
                ? AppProviderResult.Success(rawResponse: JsonSerializer.Serialize(response))
                : AppProviderResult.Failure(
                    errorCode: "ETRANZACT_TRANSMIT_ERROR",
                    errorMessage: response.Error ?? response.Message ?? "Transmission failed",
                    rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact TransmitInvoice failed for IRN {Irn}", irn);
            return AppProviderResult.Failure("ETRANZACT_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppLookupTinResult> LookupTinAsync(
        string tin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.VerifyTinAsync(
                new VerifyTinRequest { Tin = tin }, cancellationToken);

            if (!response.IsSuccess)
                return new AppLookupTinResult(false, false, null, response.Error ?? response.Message ?? "TIN lookup failed");

            var isEnrolled = string.Equals(response.Data?.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
            return new AppLookupTinResult(
                IsSuccess: true,
                IsUp: isEnrolled,
                BusinessReference: response.Data?.Id,
                ErrorMessage: isEnrolled ? null : "TIN is inactive or not enrolled on the NRS portal");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact LookupTIN failed for TIN {Tin}", tin);
            return new AppLookupTinResult(false, false, null, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<(bool IsHealthy, string? Message)> PingAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthy = await client.TestConnectionAsync(cancellationToken);
            return healthy
                ? (true, null)
                : (false, "eTranzact API did not respond successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact PingAsync failed");
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
            // Map domain Invoice to eTranzact SignInvoiceRequest
            var request = MapInvoiceToEtranzactRequest(invoice);
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
                ErrorCode: "ETRANZACT_SIGN_FAILED",
                ErrorMessage: response.Error ?? response.Message ?? "Invoice signing failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact SignInvoice failed for invoice {InvoiceId}", invoice.Id);
            return new AppSignInvoiceResult(false, null, null, "ETRANZACT_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppValidateInvoiceResult> ValidateInvoiceAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ValidateInvoiceRequest
            {
                // Map invoice fields to eTranzact format
                // TODO: Complete mapping based on ValidateInvoiceRequest structure
            };

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
                ErrorCode: "ETRANZACT_VALIDATE_FAILED",
                ErrorMessage: response.Error ?? response.Message ?? "Invoice validation failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact ValidateInvoice failed for invoice {InvoiceId}", invoice.Id);
            return new AppValidateInvoiceResult(false, "ETRANZACT_ERROR", ex.Message, null);
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
                ErrorCode: "ETRANZACT_VALIDATE_IRN_FAILED",
                ErrorMessage: response.Error ?? response.Message ?? "IRN validation failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact ValidateIRN failed for IRN {Irn}", irn);
            return new AppValidateIRNResult(false, false, "ETRANZACT_ERROR", ex.Message, null);
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
                ErrorCode: "ETRANZACT_CONFIRM_FAILED",
                ErrorMessage: response.Error ?? response.Message ?? "Invoice confirmation failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact ConfirmInvoice failed for IRN {Irn}", irn);
            return new AppConfirmInvoiceResult(false, "ETRANZACT_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppUpdateStatusResult> UpdateStatusAsync(
        string irn,
        string paymentStatus,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new UpdatePaymentStatusRequest
            {
                PaymentStatus = paymentStatus
            };

            var response = await client.UpdatePaymentStatusAsync(irn, request, cancellationToken);

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
                ErrorCode: "ETRANZACT_UPDATE_FAILED",
                ErrorMessage: response.Error ?? response.Message ?? "Status update failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact UpdateStatus failed for IRN {Irn}", irn);
            return new AppUpdateStatusResult(false, "ETRANZACT_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public Task<AppGetPurchaseInvoicesResult> GetPurchaseInvoicesAsync(
        string tin,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        // eTranzact does not support fetching purchase invoices yet
        logger.LogWarning("eTranzact GetPurchaseInvoices called but not supported");
        return Task.FromResult(new AppGetPurchaseInvoicesResult(
            IsSuccess: false,
            Count: 0,
            Items: null,
            ErrorCode: "ETRANZACT_NOT_SUPPORTED",
            ErrorMessage: "eTranzact does not support GetPurchaseInvoices operation yet"));
    }

    /// <inheritdoc />
    public Task<AppLookupWithIRNResult> LookupWithIRNAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        // eTranzact does not have a direct lookup with IRN that returns party info
        // ValidateIrnAsync only validates the IRN format/existence
        logger.LogWarning("eTranzact LookupWithIRN called but limited support");
        return Task.FromResult(new AppLookupWithIRNResult(
            IsSuccess: false,
            SupplierParty: null,
            CustomerParty: null,
            ErrorCode: "ETRANZACT_LIMITED_SUPPORT",
            ErrorMessage: "eTranzact does not support full LookupWithIRN operation (use ValidateIRN instead)"));
    }

    /// <inheritdoc />
    public Task<AppDownloadInvoiceResult> DownloadInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        // eTranzact does not support invoice download yet
        logger.LogWarning("eTranzact DownloadInvoice called but not supported");
        return Task.FromResult(new AppDownloadInvoiceResult(
            IsSuccess: false,
            EncryptedData: null,
            Iv: null,
            PublicKey: null,
            ErrorCode: "ETRANZACT_NOT_SUPPORTED",
            ErrorMessage: "eTranzact does not support DownloadInvoice operation yet",
            RawResponse: null));
    }

    /// <inheritdoc />
    public Task<AppSearchInvoiceResult> SearchInvoiceAsync(
        string businessId,
        CancellationToken cancellationToken = default)
    {
        // eTranzact does not support invoice search yet
        logger.LogWarning("eTranzact SearchInvoice called but not supported");
        return Task.FromResult(new AppSearchInvoiceResult(
            IsSuccess: false,
            Items: null,
            ErrorCode: "ETRANZACT_NOT_SUPPORTED",
            ErrorMessage: "eTranzact does not support SearchInvoice operation yet"));
    }

    /// <inheritdoc />
    public Task<AppGetEntityResult> GetEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        // eTranzact does not support entity lookup yet
        logger.LogWarning("eTranzact GetEntity called but not supported");
        return Task.FromResult(new AppGetEntityResult(
            IsSuccess: false,
            EntityData: null,
            ErrorCode: "ETRANZACT_NOT_SUPPORTED",
            ErrorMessage: "eTranzact does not support GetEntity operation yet"));
    }

    // Helper method to map domain Invoice to eTranzact SignInvoiceRequest
    private static SignInvoiceRequest MapInvoiceToEtranzactRequest(Invoice invoice)
    {
        // TODO: Implement proper mapping when SignInvoiceRequest structure is available
        // For now, return a minimal request object
        return new SignInvoiceRequest
        {
            // Map invoice fields to eTranzact format
            // This will need to be completed based on SignInvoiceRequest structure
        };
    }
}
