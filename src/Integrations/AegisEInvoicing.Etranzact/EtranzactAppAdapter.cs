using System.Text.Json;
using AndersenNigeria.Application.Abstractions.Integration;
using AegisEInvoicing.Etranzact.Contracts;
using AegisEInvoicing.Etranzact.Models.Requests;
using AndersenNigeria.SharedKernel.Enumerations;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Etranzact;

/// <summary>
/// Adapts the eTranzact integration client to the vendor-agnostic <see cref="IAppFinancialDocumentClient"/> interface.
/// Maps between APP-level request/response types and eTranzact-specific models.
/// </summary>
public sealed class EtranzactAppAdapter(
    IEtranzactClient client,
    ILogger<EtranzactAppAdapter> logger) : IAppFinancialDocumentClient
{
    /// <inheritdoc />
    public AppVendor Vendor => AppVendor.Etranzact;

    /// <inheritdoc />
    public void Configure(string baseUrl, Dictionary<string, string> credentials)
    {
        credentials.TryGetValue("ClientApiKey", out var clientApiKey);
        credentials.TryGetValue("ClientSecretKey", out var clientSecretKey);

        client.Configure(
            baseUrl,
            clientApiKey ?? string.Empty,
            clientSecretKey ?? string.Empty);

        logger.LogInformation(
            "eTranzact APP adapter configured with base URL: {BaseUrl}, ClientApiKey present: {HasKey}",
            baseUrl, !string.IsNullOrWhiteSpace(clientApiKey));
    }

    /// <inheritdoc />
    public async Task<AppResult> ValidateFinancialDocumentAsync(
        AppFinancialDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var etranzactRequest = MapToValidateInvoiceRequest(request);
            var response = await client.ValidateInvoiceAsync(etranzactRequest, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "ETRANZACT_VALIDATION_ERROR",
                errorMessage: response.Error ?? response.Message ?? "Validation failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact ValidateInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("ETRANZACT_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppResult> SignFinancialDocumentAsync(
        AppFinancialDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var etranzactRequest = MapToSignInvoiceRequest(request);
            var response = await client.SignInvoiceAsync(etranzactRequest, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "ETRANZACT_SIGN_ERROR",
                errorMessage: response.Error ?? response.Message ?? "Signing failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact SignInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("ETRANZACT_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppResult> TransmitFinancialDocumentAsync(
        AppFinancialDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var etranzactRequest = new TransmitInvoiceRequest { Irn = request.Irn };
            var response = await client.TransmitInvoiceAsync(etranzactRequest, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "ETRANZACT_TRANSMIT_ERROR",
                errorMessage: response.Error ?? response.Message ?? "Transmission failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact TransmitInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("ETRANZACT_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppResult> UpdatePaymentStatusAsync(
        AppStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var etranzactRequest = new UpdatePaymentStatusRequest
            {
                PaymentStatus = request.Status
            };

            var response = await client.UpdatePaymentStatusAsync(
                request.Irn, etranzactRequest, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "ETRANZACT_STATUS_ERROR",
                errorMessage: response.Error ?? response.Message ?? "Status update failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact UpdatePaymentStatus failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("ETRANZACT_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppResult> ConfirmFinancialDocumentAsync(
        AppFinancialDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.ConfirmInvoiceAsync(request.Irn, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "ETRANZACT_CONFIRM_ERROR",
                errorMessage: response.Error ?? response.Message ?? "Confirmation failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact ConfirmInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("ETRANZACT_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public bool SupportsReceivedInvoiceQuery => false;

    /// <inheritdoc />
    public Task<AppResult> GetReceivedInvoicesAsync(AppGetReceivedInvoicesRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AppResult.Failure("NOT_SUPPORTED", "eTranzact delivers received invoices via webhook — query the local repository instead."));

    /// <inheritdoc />
    public Task<AppResult> GetReceivedInvoiceByIrnAsync(string irn, CancellationToken cancellationToken = default)
        => Task.FromResult(AppResult.Failure("NOT_SUPPORTED", "eTranzact delivers received invoices via webhook — query the local repository instead."));

    #region Private Mapping Helpers

    private static ValidateInvoiceRequest MapToValidateInvoiceRequest(AppFinancialDocumentRequest request)
    {
        return new ValidateInvoiceRequest
        {
            BusinessId = request.BusinessId?.ToString() ?? string.Empty,
            Irn = request.Irn,
            IssueDate = request.DocumentDate,
            DueDate = request.DueDate,
            InvoiceTypeCode = request.InvoiceTypeCode,
            DocumentCurrencyCode = request.Currency,
            TaxCurrencyCode = request.Currency,
            PaymentStatus = "PENDING",
            TaxPointDate = request.DocumentDate,
            Note = request.Notes,
            OrderReference = request.Reference,
            PaymentMeans =
            [
                new EtranzactPaymentMean
                {
                    PaymentMeansCode = request.PaymentMeansCode,
                    PaymentDueDate = request.PaymentDueDate
                }
            ],
            PaymentTermsNote = request.PaymentTerms,
            BillingReference = MapBillingReferences(request.DocumentReferences),
            DispatchDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Dispatch"),
            ReceiptDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Receipt"),
            OriginatorDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Originator"),
            ContractDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Contract"),
            AdditionalDocumentReference = MapAdditionalDocumentReferences(request.DocumentReferences),
            AccountingSupplierParty = MapSupplierParty(request),
            AccountingCustomerParty = MapCustomerParty(request),
            AllowanceCharge = BuildAllowanceCharges(request.Lines),
            TaxTotal = BuildTaxTotal(request),
            LegalMonetaryTotal = new EtranzactLegalMonetaryTotal
            {
                LineExtensionAmount = (double)request.Subtotal,
                TaxExclusiveAmount = (double)request.Subtotal,
                TaxInclusiveAmount = (double)request.Total,
                PayableAmount = (double)request.Total
            },
            InvoiceLine = request.Lines.Select(MapInvoiceLine).ToList()
        };
    }

    private static SignInvoiceRequest MapToSignInvoiceRequest(AppFinancialDocumentRequest request)
    {
        return new SignInvoiceRequest
        {
            BusinessId = request.BusinessId?.ToString() ?? string.Empty,
            Irn = request.Irn,
            IssueDate = request.DocumentDate,
            DueDate = request.DueDate,
            InvoiceTypeCode = request.InvoiceTypeCode,
            DocumentCurrencyCode = request.Currency,
            TaxCurrencyCode = request.Currency,
            PaymentStatus = "PENDING",
            TaxPointDate = request.DocumentDate,
            Note = request.Notes,
            OrderReference = request.Reference,
            PaymentMeans =
            [
                new EtranzactPaymentMean
                {
                    PaymentMeansCode = request.PaymentMeansCode,
                    PaymentDueDate = request.PaymentDueDate
                }
            ],
            PaymentTermsNote = request.PaymentTerms,
            BillingReference = MapBillingReferences(request.DocumentReferences),
            DispatchDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Dispatch"),
            ReceiptDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Receipt"),
            OriginatorDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Originator"),
            ContractDocumentReference = MapSingleDocumentReference(request.DocumentReferences, "Contract"),
            AdditionalDocumentReference = MapAdditionalDocumentReferences(request.DocumentReferences),
            AccountingSupplierParty = MapSupplierParty(request),
            AccountingCustomerParty = MapCustomerParty(request),
            AllowanceCharge = BuildAllowanceCharges(request.Lines),
            TaxTotal = BuildTaxTotal(request),
            LegalMonetaryTotal = new EtranzactLegalMonetaryTotal
            {
                LineExtensionAmount = (double)request.Subtotal,
                TaxExclusiveAmount = (double)request.Subtotal,
                TaxInclusiveAmount = (double)request.Total,
                PayableAmount = (double)request.Total
            },
            InvoiceLine = request.Lines.Select(MapInvoiceLine).ToList()
        };
    }

    private static EtranzactAccountingParty MapSupplierParty(AppFinancialDocumentRequest request)
    {
        return new EtranzactAccountingParty
        {
            PartyName = request.SellerName,
            Tin = request.SellerTin,
            Email = request.SellerEmail ?? string.Empty,
            PostalAddress = new EtranzactPostalAddress
            {
                StreetName = string.Join(" ", request.SellerAddressNumber, request.SellerStreet).Trim(),
                CityName = request.SellerCity ?? string.Empty,
                State = request.SellerState,
                Lga = request.SellerLga,
                PostalZone = request.SellerPostalCode ?? string.Empty,
                Country = request.SellerCountry ?? "NG"
            }
        };
    }

    private static EtranzactAccountingParty MapCustomerParty(AppFinancialDocumentRequest request)
    {
        return new EtranzactAccountingParty
        {
            PartyName = request.BuyerName,
            Tin = request.BuyerTin,
            Email = request.BuyerEmail ?? string.Empty,
            PostalAddress = new EtranzactPostalAddress
            {
                StreetName = string.Join(" ", request.BuyerAddressNumber, request.BuyerStreet).Trim(),
                CityName = request.BuyerCity ?? string.Empty,
                State = request.BuyerState,
                Lga = request.BuyerLga,
                PostalZone = request.BuyerPostalCode ?? string.Empty,
                Country = request.BuyerCountry ?? "NG"
            }
        };
    }

    private static List<EtranzactBillingReference> MapBillingReferences(
        IReadOnlyList<AppDocumentReferenceRequest> refs)
    {
        return refs
            .Where(r => string.Equals(r.Type, "Billing", StringComparison.OrdinalIgnoreCase))
            .Select(r => new EtranzactBillingReference { Irn = r.Irn, IssueDate = r.IssueDate })
            .ToList();
    }

    private static EtranzactDocumentReference? MapSingleDocumentReference(
        IReadOnlyList<AppDocumentReferenceRequest> refs, string type)
    {
        var match = refs.FirstOrDefault(r => string.Equals(r.Type, type, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? null
            : new EtranzactDocumentReference { Irn = match.Irn, IssueDate = match.IssueDate };
    }

    private static List<EtranzactDocumentReference> MapAdditionalDocumentReferences(
        IReadOnlyList<AppDocumentReferenceRequest> refs)
    {
        return refs
            .Where(r => string.Equals(r.Type, "Additional", StringComparison.OrdinalIgnoreCase))
            .Select(r => new EtranzactDocumentReference { Irn = r.Irn, IssueDate = r.IssueDate })
            .ToList();
    }

    private static List<EtranzactAllowanceCharge> BuildAllowanceCharges(
        IReadOnlyList<AppFinancialDocumentLineItemRequest> lines)
    {
        var charges = new List<EtranzactAllowanceCharge>();
        var totalDiscount = 0d;
        var totalFee = 0d;

        foreach (var line in lines)
        {
            if (line.DiscountFeeAmount > 0)
            {
                totalDiscount += line.DiscountFeeType == "Percent"
                    ? (double)(line.UnitPrice * line.Quantity * line.DiscountFeeAmount / 100m)
                    : (double)line.DiscountFeeAmount;
            }

            if (line.AdditionalFeeAmount > 0)
            {
                totalFee += line.AdditionalFeeType == "Percent"
                    ? (double)(line.UnitPrice * line.Quantity * line.AdditionalFeeAmount / 100m)
                    : (double)line.AdditionalFeeAmount;
            }
        }

        if (totalDiscount > 0)
            charges.Add(new EtranzactAllowanceCharge { ChargeIndicator = false, Amount = totalDiscount });

        if (totalFee > 0)
            charges.Add(new EtranzactAllowanceCharge { ChargeIndicator = true, Amount = totalFee });

        return charges;
    }

    private static List<EtranzactTaxTotal> BuildTaxTotal(AppFinancialDocumentRequest request)
    {
        var taxSubtotals = request.Lines
            .Select(l => new EtranzactTaxSubtotal
            {
                TaxableAmount = (double)l.Subtotal,
                TaxAmount = (double)l.TaxAmount,
                TaxCategoryPercent = (double)l.TaxRatePercent,
                TaxCategory = new EtranzactTaxCategory
                {
                    Id = l.TaxCode,
                    Percent = (double)l.TaxRatePercent
                }
            })
            .ToList();

        return
        [
            new EtranzactTaxTotal
            {
                TaxAmount = (double)request.Lines.Sum(l => l.TaxAmount),
                TaxSubtotal = taxSubtotals
            }
        ];
    }

    private static EtranzactInvoiceLine MapInvoiceLine(AppFinancialDocumentLineItemRequest line, int index)
    {
        double discountRate = 0, discountAmount = 0;
        double feeRate = 0, feeAmount = 0;

        if (line.DiscountFeeAmount > 0)
        {
            if (line.DiscountFeeType == "Percent")
            {
                discountRate = (double)line.DiscountFeeAmount;
                discountAmount = (double)(line.UnitPrice * line.Quantity * line.DiscountFeeAmount / 100m);
            }
            else
            {
                discountAmount = (double)line.DiscountFeeAmount;
                discountRate = (double)(line.DiscountFeeAmount / (line.UnitPrice * line.Quantity) * 100);
            }
        }

        if (line.AdditionalFeeAmount > 0)
        {
            if (line.AdditionalFeeType == "Percent")
            {
                feeRate = (double)line.AdditionalFeeAmount;
                feeAmount = (double)(line.UnitPrice * line.Quantity * line.AdditionalFeeAmount / 100m);
            }
            else
            {
                feeAmount = (double)line.AdditionalFeeAmount;
            }
        }

        var isGoods = string.Equals(line.ProductCategory, "GOODS", StringComparison.OrdinalIgnoreCase);

        return new EtranzactInvoiceLine
        {
            InvoicedQuantity = (int)line.Quantity,
            LineExtensionAmount = (double)line.Subtotal,
            ProductCategory = isGoods ? line.CategoryName : null,
            ServiceCategory = !isGoods ? line.CategoryName : null,
            HsnCode = isGoods ? line.ClassificationCode : null,
            IsicCode = !isGoods ? line.ClassificationCode : null,
            DiscountRate = discountRate,
            DiscountAmount = discountAmount,
            FeeRate = feeRate,
            FeeAmount = feeAmount,
            Item = new EtranzactItem
            {
                Name = line.Description,
                Description = line.Description,
                SellersItemIdentification = $"ITEM-{index + 1}"
            },
            Price = new EtranzactPrice
            {
                PriceAmount = (double)line.UnitPrice,
                BaseQuantity = 1,
                PriceUnit = line.UnitOfMeasure
            }
        };
    }

    /// <inheritdoc />
    public async Task<(bool IsHealthy, string? Message)> PingAsync(CancellationToken cancellationToken = default)
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
            logger.LogWarning(ex, "eTranzact health check failed");
            return (false, ex.Message);
        }
    }

    #endregion
}
