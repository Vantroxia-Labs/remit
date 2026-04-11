using System.Text.Json;
using AndersenNigeria.Application.Abstractions.Integration;
using AegisEInvoicing.BlueBridge.Contracts;
using AegisEInvoicing.BlueBridge.Models.Requests;
using AndersenNigeria.SharedKernel.Enumerations;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.BlueBridge;

/// <summary>
/// Adapts the BlueBridge integration client to the vendor-agnostic <see cref="IAppFinancialDocumentClient"/> interface.
/// Maps between APP-level request/response types and BlueBridge-specific models.
/// </summary>
public sealed class BlueBridgeAppAdapter(
    IBlueBridgeClient client,
    ILogger<BlueBridgeAppAdapter> logger) : IAppFinancialDocumentClient
{
    public AppVendor Vendor => AppVendor.BlueBridge;

    public void Configure(string baseUrl, Dictionary<string, string> credentials)
    {
        credentials.TryGetValue("ApiKey", out var apiKey);

        client.Configure(
            baseUrl,
            apiKey ?? string.Empty);

        logger.LogInformation(
            "BlueBridge APP adapter configured with base URL: {BaseUrl}, ApiKey present: {HasKey}",
            baseUrl, !string.IsNullOrWhiteSpace(apiKey));
    }

    public async Task<AppResult> ValidateFinancialDocumentAsync(
        AppFinancialDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blueBridgeRequest = MapToInvoiceRequest(request);
            var response = await client.ValidateInvoiceAsync(blueBridgeRequest, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "BLUEBRIDGE_VALIDATION_ERROR",
                errorMessage: response.Message ?? "Validation failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge ValidateInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    public async Task<AppResult> SignFinancialDocumentAsync(
        AppFinancialDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blueBridgeRequest = MapToInvoiceRequest(request);
            var response = await client.SignInvoiceAsync(blueBridgeRequest, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "BLUEBRIDGE_SIGN_ERROR",
                errorMessage: response.Message ?? "Signing failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge SignInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    public async Task<AppResult> TransmitFinancialDocumentAsync(
        AppFinancialDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.TransmitInvoiceAsync(request.Irn, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "BLUEBRIDGE_TRANSMIT_ERROR",
                errorMessage: response.Message ?? "Transmission failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge TransmitInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    public async Task<AppResult> UpdatePaymentStatusAsync(
        AppStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updateRequest = new UpdateInvoiceRequest
            {
                PaymentStatus = request.Status
            };

            var response = await client.UpdateInvoiceAsync(
                request.Irn, updateRequest, cancellationToken);

            if (response.IsSuccess)
            {
                return AppResult.Success(
                    irn: request.Irn,
                    rawResponse: JsonSerializer.Serialize(response));
            }

            return AppResult.Failure(
                errorCode: "BLUEBRIDGE_STATUS_ERROR",
                errorMessage: response.Message ?? "Status update failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge UpdateInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("BLUEBRIDGE_ERROR", ex.Message);
        }
    }

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
                errorCode: "BLUEBRIDGE_CONFIRM_ERROR",
                errorMessage: response.Message ?? "Confirmation failed",
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge ConfirmInvoice failed for IRN {Irn}", request.Irn);
            return AppResult.Failure("BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public bool SupportsReceivedInvoiceQuery => false;

    /// <inheritdoc />
    public Task<AppResult> GetReceivedInvoicesAsync(AppGetReceivedInvoicesRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AppResult.Failure("NOT_SUPPORTED", "BlueBridge delivers received invoices via webhook — query the local repository instead."));

    /// <inheritdoc />
    public Task<AppResult> GetReceivedInvoiceByIrnAsync(string irn, CancellationToken cancellationToken = default)
        => Task.FromResult(AppResult.Failure("NOT_SUPPORTED", "BlueBridge delivers received invoices via webhook — query the local repository instead."));

    #region Private Mapping Helpers

    private static BlueBridgeInvoiceRequest MapToInvoiceRequest(AppFinancialDocumentRequest request)
    {
        return new BlueBridgeInvoiceRequest
        {
            BusinessId = request.BusinessId?.ToString() ?? string.Empty,
            Irn = request.Irn,
            IssueDate = request.DocumentDate,
            DueDate = request.DueDate,
            InvoiceTypeCode = request.InvoiceTypeCode,
            InvoiceKind = request.InvoiceKind,
            DocumentCurrencyCode = request.Currency,
            TaxCurrencyCode = request.Currency,
            PaymentStatus = "PENDING",
            OrderReference = request.DocumentNumber,
            AccountingSupplierParty = new BlueBridgeAccountingParty
            {
                PartyName = request.SellerName,
                Tin = request.SellerTin,
                Email = request.SellerEmail ?? string.Empty,
                PostalAddress = new BlueBridgePostalAddress
                {
                    StreetName = request.SellerStreet ?? string.Empty,
                    CityName = request.SellerCity ?? string.Empty,
                    State = request.SellerState,
                    Lga = request.SellerLga,
                    PostalZone = request.SellerPostalCode ?? string.Empty,
                    Country = request.SellerCountry ?? "NG"
                }
            },
            AccountingCustomerParty = new BlueBridgeAccountingParty
            {
                PartyName = request.BuyerName,
                Tin = request.BuyerTin,
                Email = request.BuyerEmail ?? string.Empty,
                PostalAddress = new BlueBridgePostalAddress
                {
                    StreetName = request.BuyerStreet ?? string.Empty,
                    CityName = request.BuyerCity ?? string.Empty,
                    State = request.BuyerState,
                    Lga = request.BuyerLga,
                    PostalZone = request.BuyerPostalCode ?? string.Empty,
                    Country = request.BuyerCountry ?? "NG"
                }
            },
            AllowanceCharge = BuildAllowanceCharges(request.Lines),
            TaxTotal = BuildTaxTotal(request),
            LegalMonetaryTotal = new BlueBridgeLegalMonetaryTotal
            {
                LineExtensionAmount = (double)request.Subtotal,
                TaxExclusiveAmount = (double)request.Subtotal,
                TaxInclusiveAmount = (double)request.Total,
                PayableAmount = (double)request.Total
            },
            InvoiceLine = [.. request.Lines.Select(MapInvoiceLine)]
        };
    }

    private static List<BlueBridgeTaxTotal> BuildTaxTotal(AppFinancialDocumentRequest request)
    {
        var taxSubtotals = request.Lines
            .Select(l => new BlueBridgeTaxSubtotal
            {
                TaxableAmount = (double)l.Subtotal,
                TaxAmount = (double)l.TaxAmount,
                TaxCategoryPercent = (double)l.TaxRatePercent,
                TaxCategory = new BlueBridgeTaxCategory
                {
                    Id = l.TaxCode ?? "VAT",
                    Percent = (double)l.TaxRatePercent,
                    TaxScheme = new BlueBridgeTaxScheme { Id = l.TaxCode ?? "VAT" }
                }
            })
            .ToList();

        return
        [
            new BlueBridgeTaxTotal
            {
                TaxAmount = (double)request.Lines.Sum(l => l.TaxAmount),
                TaxSubtotal = taxSubtotals
            }
        ];
    }

    private static List<BlueBridgeAllowanceCharge> BuildAllowanceCharges(
        IReadOnlyList<AppFinancialDocumentLineItemRequest> lines)
    {
        var charges = new List<BlueBridgeAllowanceCharge>();
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
            charges.Add(new BlueBridgeAllowanceCharge { ChargeIndicator = false, Amount = totalDiscount });

        if (totalFee > 0)
            charges.Add(new BlueBridgeAllowanceCharge { ChargeIndicator = true, Amount = totalFee });

        return charges;
    }

    private static BlueBridgeInvoiceLine MapInvoiceLine(AppFinancialDocumentLineItemRequest line, int index)
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
                feeRate = (double)(line.AdditionalFeeAmount / (line.UnitPrice * line.Quantity) * 100);
            }
        }

        var isGoods = string.Equals(line.ProductCategory, "GOODS", StringComparison.OrdinalIgnoreCase);

        return new BlueBridgeInvoiceLine
        {
            InvoicedQuantity = (int)line.Quantity,
            LineExtensionAmount = (double)line.Subtotal,
            HsnCode = isGoods ? line.ClassificationCode : null,
            IsicCode = !isGoods ? line.ClassificationCode : null,
            ProductCategory = isGoods ? (line.CategoryName ?? line.Description) : null,
            ServiceCategory = !isGoods ? (line.CategoryName ?? line.Description) : null,
            DiscountRate = discountRate > 0 ? discountRate : null,
            DiscountAmount = discountAmount > 0 ? discountAmount : null,
            FeeRate = feeRate > 0 ? feeRate : null,
            FeeAmount = feeAmount > 0 ? feeAmount : null,
            Item = new BlueBridgeItem
            {
                Name = line.Description,
                Description = line.Description,
                SellersItemIdentification = $"ITEM-{index + 1}"
            },
            Price = new BlueBridgePrice
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
            var response = await client.HealthCheckAsync(cancellationToken);
            return response.IsHealthy
                ? (true, null)
                : (false, $"BlueBridge API returned status: {response.Status ?? "unknown"}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BlueBridge health check failed");
            return (false, ex.Message);
        }
    }

    #endregion
}
