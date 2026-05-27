using System.Text.Json;
using AegisEInvoicing.Application.Common.Extensions;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Interswitch.Interfaces;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithTIN;
using AegisEInvoicing.Interswitch.Models.Requests.TransmitInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.SignInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.ValidateInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.ValidateIRN;
using AegisEInvoicing.Interswitch.Models.Requests.ConfirmInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.UpdateStatus;
using AegisEInvoicing.Interswitch.Models.Requests.DownloadInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.SearchInvoice;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithIRN;
using AegisEInvoicing.Interswitch.Models.Requests.GetEntity;
using AegisEInvoicing.Interswitch.Models.Requests.GetPurchaseInvoices;
using AegisEInvoicing.Interswitch.Models.Responses.GetPurchaseInvoices;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Interswitch;

/// <summary>
/// Adapts <see cref="IInterswitchHttpClient"/> to the vendor-agnostic
/// <see cref="IAccessPointProviderClient"/>
///
/// Credential JSON schema:
/// { "clientId": "...", "clientSecret": "...", "tokenEndpoint": "/Api/SwitchTax/Token" }
/// </summary>
public sealed class InterswitchAppAdapter(
    IInterswitchHttpClient client,
    ILogger<InterswitchAppAdapter> logger) : IAccessPointProviderClient
{
    /// <inheritdoc />
    public string ProviderCode => "interswitch";

    /// <inheritdoc />
    public string DisplayName => "Interswitch";

    /// <inheritdoc />
    public bool SupportsLookupTin => true;

    /// <inheritdoc />
    public async Task<AppProviderResult> TransmitAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.TransmitInvoiceAsync(
                new TransmitInvoiceRequest { IRN = irn },
                cancellationToken);

            if (response?.Data?.Code == 200)
            {
                return AppProviderResult.Success(rawResponse: JsonSerializer.Serialize(response));
            }

            var errorMessage = response?.Data?.Error?.PublicMessage
                            ?? response?.Data?.Error?.Details
                            ?? "Transmission failed";

            return AppProviderResult.Failure(
                errorCode: $"INTERSWITCH_{response?.Data?.Code ?? 0}",
                errorMessage: errorMessage,
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch TransmitInvoice failed for IRN {Irn}", irn);
            return AppProviderResult.Failure("INTERSWITCH_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppLookupTinResult> LookupTinAsync(
        string tin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.LookupWithTINAsync(
                new LookupWithTINRequest { TIN = tin },
                cancellationToken);

            var isUp = response?.Data?.Data?.IsUp ?? false;
            var businessRef = response?.Data?.Data?.BusinessReference;

            return new AppLookupTinResult(
                IsSuccess: true,
                IsUp: isUp,
                BusinessReference: businessRef,
                ErrorMessage: isUp ? null : "TIN is invalid or not enrolled on the NRS portal");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch LookupTIN failed for TIN {Tin}", tin);
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
                : (false, "Interswitch API did not respond successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Interswitch health check failed");
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
            // Map domain Invoice entity to Interswitch SignInvoiceRequest using extension methods
            var signingRequest = new SignInvoiceRequest
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
                InvoiceDeliveryPeriod = invoice.DeliveryPeriod.ToSigningInvoiceDeliveryPeriod(),
                BillingReference = invoice.BillingReferences.ToList().ToSigningBillingReference(),
                DispatchDocumentReference = invoice.DispatchDocumentReference!.ToSigningDispatchDocumentReference(),
                ReceiptDocumentReference = invoice.ReceiptDocumentReference!.ToSigningReceiptDocumentReference(),
                OriginatorDocumentReference = invoice.OriginatorDocumentReference!.ToSigningOriginatorDocumentReference(),
                ContractDocumentReference = invoice.ContractDocumentReference!.ToSigningContractDocumentReference(),
                DocumentReference = invoice.AdditionalDocumentReferences.ToList().ToSigningAddtionalDocumentReference(),
                AccountingCustomerParty = invoice.Party.ToSigningAccountingCustomerParty(),
                AccountingSupplierParty = invoice.Business.ToSigningAccountingSupplierParty(),
                PaymentMeans = invoice.PaymentMeans!.ToSigningPaymentMeans(invoice.IssueDate.AddDays(7)),
                PaymentTermsNote = invoice.PaymentTerms,
                AllowanceCharge = invoice.InvoiceLine.ToList().ToSigningAllowanceCharge(),
                TaxTotal = invoice.InvoiceLine.ToList().ToSigningTaxTotal(),
                LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToSigningLegalMonetaryTotal(),
                InvoiceLine = invoice.InvoiceLine.ToList().ToSigningInvoiceLine(invoice.Currency.Code)
            };

            var response = await client.SignInvoiceAsync(signingRequest, cancellationToken);

            if (response?.Code == 201 || response?.Code == 200)
            {
                return new AppSignInvoiceResult(
                    IsSuccess: true,
                    Irn: invoice.Irn?.Value,
                    SignedDate: DateTime.UtcNow,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            var errorMessage = response?.Error?.PublicMessage
                            ?? response?.Error?.Details
                            ?? "Invoice signing failed";

            return new AppSignInvoiceResult(
                IsSuccess: false,
                Irn: null,
                SignedDate: null,
                ErrorCode: $"INTERSWITCH_{response?.Code ?? 0}",
                ErrorMessage: errorMessage,
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch SignInvoice failed for invoice {InvoiceId}", invoice.Id);
            return new AppSignInvoiceResult(false, null, null, "INTERSWITCH_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppValidateInvoiceResult> ValidateInvoiceAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ValidateInvoiceRequest just takes an invoice payload object
            var validateRequest = new ValidateInvoiceRequest
            {
                InvoicePayload = new SignInvoiceRequest
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
                    InvoiceDeliveryPeriod = invoice.DeliveryPeriod.ToSigningInvoiceDeliveryPeriod(),
                    BillingReference = invoice.BillingReferences.ToList().ToSigningBillingReference(),
                    DispatchDocumentReference = invoice.DispatchDocumentReference!.ToSigningDispatchDocumentReference(),
                    ReceiptDocumentReference = invoice.ReceiptDocumentReference!.ToSigningReceiptDocumentReference(),
                    OriginatorDocumentReference = invoice.OriginatorDocumentReference!.ToSigningOriginatorDocumentReference(),
                    ContractDocumentReference = invoice.ContractDocumentReference!.ToSigningContractDocumentReference(),
                    DocumentReference = invoice.AdditionalDocumentReferences.ToList().ToSigningAddtionalDocumentReference(),
                    AccountingCustomerParty = invoice.Party.ToSigningAccountingCustomerParty(),
                    AccountingSupplierParty = invoice.Business.ToSigningAccountingSupplierParty(),
                    PaymentMeans = invoice.PaymentMeans!.ToSigningPaymentMeans(invoice.IssueDate.AddDays(7)),
                    PaymentTermsNote = invoice.PaymentTerms,
                    AllowanceCharge = invoice.InvoiceLine.ToList().ToSigningAllowanceCharge(),
                    TaxTotal = invoice.InvoiceLine.ToList().ToSigningTaxTotal(),
                    LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToSigningLegalMonetaryTotal(),
                    InvoiceLine = invoice.InvoiceLine.ToList().ToSigningInvoiceLine(invoice.Currency.Code)
                }
            };

            var response = await client.ValidateInvoiceAsync(validateRequest, cancellationToken);

            if (response?.Code == 200 || response?.Code == 201)
            {
                return new AppValidateInvoiceResult(
                    IsSuccess: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            var errorMessage = response?.Error?.PublicMessage
                            ?? response?.Error?.Details
                            ?? "Invoice validation failed";

            return new AppValidateInvoiceResult(
                IsSuccess: false,
                ErrorCode: $"INTERSWITCH_{response?.Code ?? 0}",
                ErrorMessage: errorMessage,
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch ValidateInvoice failed for invoice {InvoiceId}", invoice.Id);
            return new AppValidateInvoiceResult(false, "INTERSWITCH_ERROR", ex.Message, null);
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
            var request = new ValidateIRNRequest
            {
                IRN = irn,
                InvoiceReference = invoiceReference,
                BusinessId = businessId
            };

            var response = await client.ValidateIRNAsync(request, cancellationToken);

            if (response?.Code == 200)
            {
                return new AppValidateIRNResult(
                    IsSuccess: true,
                    IsValid: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            var errorMessage = response?.Error?.PublicMessage
                            ?? response?.Error?.Details
                            ?? "IRN validation failed";

            return new AppValidateIRNResult(
                IsSuccess: false,
                IsValid: false,
                ErrorCode: $"INTERSWITCH_{response?.Code ?? 0}",
                ErrorMessage: errorMessage,
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch ValidateIRN failed for IRN {Irn}", irn);
            return new AppValidateIRNResult(false, false, "INTERSWITCH_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppConfirmInvoiceResult> ConfirmInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ConfirmInvoiceRequest { IRN = irn };
            var response = await client.ConfirmInvoiceAsync(request, cancellationToken);

            // ConfirmInvoiceWrappedResponse has different structure
            var isSuccess = response?.Success ?? false;

            if (isSuccess)
            {
                return new AppConfirmInvoiceResult(
                    IsSuccess: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            return new AppConfirmInvoiceResult(
                IsSuccess: false,
                ErrorCode: "INTERSWITCH_CONFIRM_FAILED",
                ErrorMessage: "Invoice confirmation failed",
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch ConfirmInvoice failed for IRN {Irn}", irn);
            return new AppConfirmInvoiceResult(false, "INTERSWITCH_ERROR", ex.Message, null);
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
            var request = new UpdateStatusRequest
            {
                IRN = irn,
                PaymentStatus = paymentStatus,
                Reference = reference,
                Amount = amount
            };

            var response = await client.UpdateStatusAsync(request, cancellationToken);

            if (response?.Code == 200 || response?.Code == 201)
            {
                return new AppUpdateStatusResult(
                    IsSuccess: true,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            var errorMessage = response?.Error?.PublicMessage
                            ?? response?.Error?.Details
                            ?? "Status update failed";

            return new AppUpdateStatusResult(
                IsSuccess: false,
                ErrorCode: $"INTERSWITCH_{response?.Code ?? 0}",
                ErrorMessage: errorMessage,
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch UpdateStatus failed for IRN {Irn}", irn);
            return new AppUpdateStatusResult(false, "INTERSWITCH_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppGetPurchaseInvoicesResult> GetPurchaseInvoicesAsync(
        string tin,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetPurchaseInvoicesRequest
            {
                Tin = tin,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            };

            var response = await client.GetPurchaseInvoicesAsync(request, cancellationToken);

            if (response != null)
            {
                var items = response.Data?.Select(MapPurchaseInvoiceItem).ToList();

                return new AppGetPurchaseInvoicesResult(
                    IsSuccess: true,
                    Count: response.Count,
                    Items: items,
                    ErrorCode: null,
                    ErrorMessage: null);
            }

            return new AppGetPurchaseInvoicesResult(
                IsSuccess: false,
                Count: 0,
                Items: null,
                ErrorCode: "INTERSWITCH_NO_RESPONSE",
                ErrorMessage: "Get purchase invoices failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch GetPurchaseInvoices failed for TIN {Tin}", tin);
            return new AppGetPurchaseInvoicesResult(false, 0, null, "INTERSWITCH_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppLookupWithIRNResult> LookupWithIRNAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new LookupWithIRNRequest { IRN = irn };
            var response = await client.LookupWithIRNAsync(request, cancellationToken);

            if (response != null && response.Success)
            {
                return new AppLookupWithIRNResult(
                    IsSuccess: true,
                    SupplierParty: null, // TODO: Map from response.Data when needed
                    CustomerParty: null, // TODO: Map from response.Data when needed
                    ErrorCode: null,
                    ErrorMessage: null);
            }

            return new AppLookupWithIRNResult(
                IsSuccess: false,
                SupplierParty: null,
                CustomerParty: null,
                ErrorCode: "INTERSWITCH_LOOKUP_FAILED",
                ErrorMessage: "Lookup with IRN failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch LookupWithIRN failed for IRN {Irn}", irn);
            return new AppLookupWithIRNResult(false, null, null, "INTERSWITCH_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppDownloadInvoiceResult> DownloadInvoiceAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DownloadInvoiceRequest { IRN = irn };
            var response = await client.DownloadInvoiceAsync(request, cancellationToken);

            if (response?.Code == 200)
            {
                return new AppDownloadInvoiceResult(
                    IsSuccess: true,
                    EncryptedData: null, // TODO: Map from response.Data
                    Iv: null,
                    PublicKey: null,
                    ErrorCode: null,
                    ErrorMessage: null,
                    RawResponse: JsonSerializer.Serialize(response));
            }

            var errorMessage = response?.Error?.PublicMessage
                            ?? response?.Error?.Details
                            ?? "Download invoice failed";

            return new AppDownloadInvoiceResult(
                IsSuccess: false,
                EncryptedData: null,
                Iv: null,
                PublicKey: null,
                ErrorCode: $"INTERSWITCH_{response?.Code ?? 0}",
                ErrorMessage: errorMessage,
                RawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch DownloadInvoice failed for IRN {Irn}", irn);
            return new AppDownloadInvoiceResult(false, null, null, null, "INTERSWITCH_ERROR", ex.Message, null);
        }
    }

    /// <inheritdoc />
    public async Task<AppSearchInvoiceResult> SearchInvoiceAsync(
        string businessId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SearchInvoiceRequest { IRN = businessId };
            var response = await client.SearchInvoiceAsync(request, cancellationToken);

            if (response?.Code == 200)
            {
                // TODO: Map response.Data to AppInvoiceSearchItem list
                return new AppSearchInvoiceResult(
                    IsSuccess: true,
                    Items: new List<AppInvoiceSearchItem>(),
                    ErrorCode: null,
                    ErrorMessage: null);
            }

            var errorMessage = response?.Error?.PublicMessage
                            ?? response?.Error?.Details
                            ?? "Search invoice failed";

            return new AppSearchInvoiceResult(
                IsSuccess: false,
                Items: null,
                ErrorCode: $"INTERSWITCH_{response?.Code ?? 0}",
                ErrorMessage: errorMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch SearchInvoice failed for business {BusinessId}", businessId);
            return new AppSearchInvoiceResult(false, null, "INTERSWITCH_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppGetEntityResult> GetEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetEntityRequest { EntityId = entityId };
            var response = await client.GetEntityAsync(request, cancellationToken);

            if (response?.Code == 200)
            {
                return new AppGetEntityResult(
                    IsSuccess: true,
                    EntityData: response.Data,
                    ErrorCode: null,
                    ErrorMessage: null);
            }

            var errorMessage = response?.Error?.PublicMessage
                            ?? response?.Error?.Details
                            ?? "Get entity failed";

            return new AppGetEntityResult(
                IsSuccess: false,
                EntityData: null,
                ErrorCode: $"INTERSWITCH_{response?.Code ?? 0}",
                ErrorMessage: errorMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch GetEntity failed for entity {EntityId}", entityId);
            return new AppGetEntityResult(false, null, "INTERSWITCH_ERROR", ex.Message);
        }
    }

    // Helper method to map Interswitch PurchaseInvoiceItem to App-level type
    private static AppPurchaseInvoiceItem MapPurchaseInvoiceItem(PurchaseInvoiceItem item)
    {
        return new AppPurchaseInvoiceItem(
            BusinessId: item.BusinessId,
            IRN: item.IRN,
            CustomizationId: item.CustomizationId,
            InvoiceTypeCode: item.InvoiceTypeCode,
            IssueDate: item.IssueDate,
            IssueTime: item.IssueTime,
            DueDate: item.DueDate,
            DocumentCurrencyCode: item.DocumentCurrencyCode,
            TaxCurrencyCode: item.TaxCurrencyCode,
            PaymentStatus: item.PaymentStatus,
            EntryStatus: item.EntryStatus,
            SyncDate: item.SyncDate,
            SupplierParty: item.SupplierParty != null ? MapPartyInfo(item.SupplierParty) : null,
            CustomerParty: item.CustomerParty != null ? MapPartyInfo(item.CustomerParty) : null,
            LegalMonetaryTotal: item.LegalMonetaryTotal != null
                ? new AppMonetaryTotal(
                    item.LegalMonetaryTotal.LineExtensionAmount ?? 0,
                    item.LegalMonetaryTotal.TaxExclusiveAmount ?? 0,
                    item.LegalMonetaryTotal.TaxInclusiveAmount ?? 0,
                    item.TotalTaxAmount,
                    item.LegalMonetaryTotal.PayableAmount ?? 0,
                    item.PaidAmount,
                    item.PayableRoundingAmount)
                : null,
            TaxTotal: item.TaxTotal?.SelectMany(t =>
                t.TaxSubtotal?.Select(sub => new AppTaxTotal(
                    sub.TaxSchemeId ?? string.Empty,
                    sub.TaxableAmount,
                    sub.TaxAmount,
                    sub.TaxCategoryId,
                    sub.Percent)) ?? Enumerable.Empty<AppTaxTotal>()).ToList(),
            InvoiceLines: item.InvoiceLines?.Select(l => new AppInvoiceLine(
                l.InvoicedQuantity,
                l.LineExtensionAmount,
                l.Item?.Name ?? string.Empty,
                l.Item?.Description ?? string.Empty,
                l.Price?.PriceAmount ?? 0,
                l.Price?.BaseQuantity ?? 0,
                l.Price?.PriceUnit ?? string.Empty,
                l.HsnCode,
                l.ProductCategory,
                l.IsicCode,
                l.ServiceCategory,
                l.DiscountRate,
                l.DiscountAmount,
                l.FeeRate,
                l.FeeAmount)).ToList(),
            Note: item.Note,
            BuyerReference: item.BuyerReference,
            AccountingCost: item.AccountingCost,
            InvoiceDeliveryPeriod: item.InvoiceDeliveryPeriod != null
                ? new AppDeliveryPeriod(item.InvoiceDeliveryPeriod.StartDate, item.InvoiceDeliveryPeriod.EndDate)
                : null,
            PaymentMeans: item.PaymentMeans?.Select(pm => new AppPaymentMeans(
                pm.PaymentMeansCode ?? string.Empty,
                pm.PayeeFinancialAccount?.Id,
                pm.PaymentChannelCode)).ToList(),
            PaymentTermsNote: item.PaymentTermsNote
        );
    }

    private static AppPartyInfo MapPartyInfo(PurchaseInvoiceParty party)
    {
        return new AppPartyInfo(
            Id: party.Id,
            PartyName: party.PartyName,
            TIN: party.TIN,
            BRN: party.BRN,
            BusinessDescription: party.BusinessDescription,
            Email: party.Email,
            Telephone: party.Telephone,
            PostalAddressId: party.PostalAddressId,
            Address: party.Address != null ? new AppAddress(
                party.Address.StreetName,
                party.Address.AdditionalStreetName,
                party.Address.CityName,
                party.Address.PostalZone,
                party.Address.CountrySubentity,
                party.Address.CountryIdentificationCode
            ) : null
        );
    }

    /// <inheritdoc />
    /// <remarks>
    /// Expected credentials JSON schema (stored encrypted in AppProviderConfiguration.EncryptedCredentials):
    /// <code>{ "clientId": "...", "clientSecret": "..." }</code>
    /// The <c>tokenEndpoint</c> field is optional; defaults to <c>/Api/SwitchTax/Token</c>.
    /// The base URL comes from <c>AppProviderConfiguration.BaseUrl</c> (or <c>SandboxBaseUrl</c>).
    /// Credentials are fetched from the DB by AppProviderRouter.GetProviderAsync() on every request.
    /// </remarks>
    public void Configure(string baseUrl, string? credentialsJson)
    {
        string clientId = string.Empty, clientSecret = string.Empty;
        string tokenEndpoint = "/Api/SwitchTax/Token";

        if (credentialsJson is null)
            throw new InvalidOperationException(
                "Interswitch credentials are not configured. " +
                "Please store a credentials JSON blob with 'clientId' and 'clientSecret' " +
                "in AppProviderConfiguration.EncryptedCredentials for AdapterKey='interswitch'.");

        using var doc = JsonDocument.Parse(credentialsJson);
        var root = doc.RootElement;

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name.Equals("clientId", StringComparison.OrdinalIgnoreCase))
                clientId = prop.Value.GetString() ?? string.Empty;
            else if (prop.Name.Equals("clientSecret", StringComparison.OrdinalIgnoreCase))
                clientSecret = prop.Value.GetString() ?? string.Empty;
            else if (prop.Name.Equals("tokenEndpoint", StringComparison.OrdinalIgnoreCase))
                tokenEndpoint = prop.Value.GetString() ?? tokenEndpoint;
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException(
                "Interswitch credentials JSON is missing 'clientId' or 'clientSecret'. " +
                "Expected schema: { \"clientId\": \"...\", \"clientSecret\": \"...\" }");

        client.Configure(baseUrl, clientId, clientSecret, tokenEndpoint);
    }
}
