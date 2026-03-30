using AegisEInvoicing.Portal.API.Models.Invoice.Request;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllCountries;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetCurrencies;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetInvoiceType;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetPaymentMeans;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetTaxCategories;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetServiceCodes;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetVatExemptions;
using FirsResponseCurrency = AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetCurrencies.Currency;
using FirsResponseInvoiceType = AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetInvoiceType.InvoiceType;
using FirsResponsePaymentMeans = AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetPaymentMeans.PaymentMeans;
using FirsResponseTaxCategory = AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetTaxCategories.TaxCategory;
using FirsResponseCountry = AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllCountries.Country;
using FirsResponseServiceCode = AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetServiceCodes.ServiceCode;
using FirsResponseVatExemption = AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetVatExemptions.VatExemption;

namespace AegisEInvoicing.Portal.API.Mappings;

/// <summary>
/// Provides mapping functionality between FIRS ValidateInvoiceDataRequest and Invoice CreateInvoiceRequest
/// </summary>
public static class InvoiceDataMapping
{
    /// <summary>
    /// Maps ValidateInvoiceDataRequest from FIRS to CreateInvoiceRequest for invoice database insertion
    /// </summary>
    /// <param name="firsRequest">The FIRS validate invoice data request</param>
    /// <param name="partyId">The party ID for the invoice</param>
    /// <returns>CreateInvoiceRequest that can be used to insert into invoice database</returns>
    public static CreateInvoiceRequest MapToCreateInvoiceRequest(
        ValidateInvoiceDataRequest firsRequest, 
        Guid partyId)
    {
        ArgumentNullException.ThrowIfNull(firsRequest);

        return new CreateInvoiceRequest
        {
            PartyId = partyId,
            IssueDate = firsRequest.IssueDate,
            InvoiceType = new InvoiceTypeDto
            {
                Name = MapInvoiceTypeCodeToName(firsRequest.InvoiceTypeCode),
                Code = int.TryParse(firsRequest.InvoiceTypeCode, out var code) ? code : 0
            },
            Currency = new CurrencyDto
            {
                Name = MapCurrencyCodeToName(firsRequest.DocumentCurrencyCode),
                Code = firsRequest.DocumentCurrencyCode
            },
            DeliveryPeriod = new DeliveryPeriodDto
            {
                StartDate = firsRequest.InvoiceDeliveryPeriod?.StartDate ?? firsRequest.IssueDate,
                EndDate = firsRequest.InvoiceDeliveryPeriod?.EndDate ?? firsRequest.DueDate ?? firsRequest.IssueDate
            },
            PaymentMeans = new PaymentMeansDto
            {
                Code = firsRequest.PaymentMeans.FirstOrDefault()?.PaymentMeansCode ?? "01",
                Name = MapPaymentMeansCodeToName(firsRequest.PaymentMeans.FirstOrDefault()?.PaymentMeansCode ?? "01")
            },
            DueDate = firsRequest.DueDate,
            Note = firsRequest.Note,
            PaymentReference = firsRequest.BuyerReference,
            PaymentTerms = firsRequest.PaymentTermsNote,
            InvoiceItems = MapInvoiceLines(firsRequest.InvoiceLine)
        };
    }

    /// <summary>
    /// Maps CreateInvoiceRequest to ValidateInvoiceDataRequest for FIRS validation
    /// </summary>
    /// <param name="createRequest">The create invoice request</param>
    /// <param name="businessId">The business ID for FIRS</param>
    /// <param name="irn">The invoice reference number</param>
    /// <returns>ValidateInvoiceDataRequest for FIRS validation</returns>
    public static ValidateInvoiceDataRequest MapToValidateInvoiceDataRequest(
        CreateInvoiceRequest createRequest,
        string businessId,
        string irn)
    {
        ArgumentNullException.ThrowIfNull(createRequest);

        return new ValidateInvoiceDataRequest
        {
            BusinessId = businessId,
            Irn = irn,
            IssueDate = createRequest.IssueDate,
            DueDate = createRequest.DueDate,
            InvoiceTypeCode = createRequest.InvoiceType.Code.ToString(),
            DocumentCurrencyCode = createRequest.Currency.Code,
            Note = createRequest.Note,
            BuyerReference = createRequest.PaymentReference,
            PaymentTermsNote = createRequest.PaymentTerms,
            InvoiceDeliveryPeriod = new InvoiceDeliveryPeriod
            {
                StartDate = createRequest.DeliveryPeriod.StartDate,
                EndDate = createRequest.DeliveryPeriod.EndDate
            },
            PaymentMeans = new List<PaymentMean>
            {
                new()
                {
                    PaymentMeansCode = createRequest.PaymentMeans.Code,
                    PaymentDueDate = createRequest.DueDate
                }
            },
            // Note: These would need to be populated from business context
            AccountingSupplierParty = new AccountingSupplierParty(),
            AccountingCustomerParty = new AccountingCustomerParty(),
            LegalMonetaryTotal = new LegalMonetaryTotal(),
            InvoiceLine = new List<InvoiceLine>()
        };
    }

    /// <summary>
    /// Maps FIRS invoice lines to CreateInvoiceItemDto list
    /// </summary>
    private static List<CreateInvoiceItemDto> MapInvoiceLines(List<InvoiceLine> firsInvoiceLines)
    {
        return firsInvoiceLines?.Select(line => new CreateInvoiceItemDto
        {
            BusinessItemId = Guid.NewGuid(), // This would need to be resolved from business context
            Quantity = line.InvoicedQuantity,
            DiscountFee = line.DiscountAmount > 0 ? new DiscountFeeDto
            {
                Amount = line.DiscountAmount,
                Code = line.DiscountRate > 0 ? FeeStandardUnit.Percent : FeeStandardUnit.NGN
            } : null,
            AdditionalFee = line.FeeAmount > 0 ? new AdditionalFeeDto
            {
                Amount = line.FeeAmount,
                Code = line.FeeRate > 0 ? FeeStandardUnit.Percent : FeeStandardUnit.NGN
            } : null
        }).ToList() ?? new List<CreateInvoiceItemDto>();
    }

    /// <summary>
    /// Maps invoice type code to display name
    /// </summary>
    private static string MapInvoiceTypeCodeToName(string code)
    {
        return code switch
        {
            "01" => "Tax Invoice",
            "02" => "Credit Note", 
            "03" => "Debit Note",
            "04" => "Proforma Invoice",
            _ => "Standard Invoice"
        };
    }

    /// <summary>
    /// Maps currency code to display name
    /// </summary>
    private static string MapCurrencyCodeToName(string code)
    {
        return code switch
        {
            "NGN" => "Nigerian Naira",
            "USD" => "US Dollar",
            "EUR" => "Euro",
            "GBP" => "British Pound",
            _ => code
        };
    }

    /// <summary>
    /// Maps payment means code to display name
    /// </summary>
    private static string MapPaymentMeansCodeToName(string code)
    {
        return code switch
        {
            "01" => "Cash",
            "02" => "Bank Transfer",
            "03" => "Cheque",
            "04" => "Credit Card",
            "05" => "Debit Card",
            _ => "Other"
        };
    }
}

/// <summary>
/// Unified DTOs that align with both internal structure and FIRS response format
/// </summary>
public static class UnifiedResponseDtos
{
    /// <summary>
    /// Unified Currency DTO that works with both internal and FIRS systems
    /// </summary>
    public record CurrencyResponseDto
    {
        public string Code { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? Symbol { get; init; }
        public string? SymbolNative { get; init; }
        public int DecimalDigits { get; init; }
        public double Rounding { get; init; }
        public string? NamePlural { get; init; }
    }

    /// <summary>
    /// Unified Invoice Type DTO that works with both internal and FIRS systems
    /// </summary>
    public record InvoiceTypeResponseDto
    {
        public string Code { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string Value { get; init; } = null!;
    }

    /// <summary>
    /// Unified Payment Means DTO that works with both internal and FIRS systems
    /// </summary>
    public record PaymentMeansResponseDto
    {
        public string Code { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string Value { get; init; } = null!;
    }

    /// <summary>
    /// Unified Tax Category DTO that works with both internal and FIRS systems
    /// </summary>
    public record TaxCategoryResponseDto
    {
        public string Code { get; init; } = null!;
        public string Value { get; init; } = null!;
        public string Percent { get; init; } = null!;
    }

    /// <summary>
    /// Unified Country DTO that works with both internal and FIRS systems
    /// </summary>
    public record CountryResponseDto
    {
        public string Name { get; init; } = null!;
        public string Alpha2 { get; init; } = null!;
        public string Alpha3 { get; init; } = null!;
        public string CountryCode { get; init; } = null!;
        public string Iso31662 { get; init; } = null!;
        public string Region { get; init; } = null!;
        public string SubRegion { get; init; } = null!;
        public string? IntermediateRegion { get; init; }
        public string RegionCode { get; init; } = null!;
        public string SubRegionCode { get; init; } = null!;
        public string? IntermediateRegionCode { get; init; }
    }

    /// <summary>
    /// Unified Service Code DTO that works with both internal and FIRS systems
    /// </summary>
    public record ServiceCodeResponseDto
    {
        public string Code { get; init; } = null!;
        public string Description { get; init; } = null!;
    }

    /// <summary>
    /// Unified VAT Exemption DTO that works with both internal and FIRS systems
    /// </summary>
    public record VatExemptionResponseDto
    {
        public string HeadingNo { get; init; } = null!;
        public string HarmonizedSystemCode { get; init; } = null!;
        public string TariffCategory { get; init; } = null!;
        public string Tariff { get; init; } = null!;
        public string Description { get; init; } = null!;
    }

    /// <summary>
    /// Maps FIRS Currency to Unified CurrencyResponseDto
    /// </summary>
    public static CurrencyResponseDto MapCurrency(FirsResponseCurrency firsCurrency)
    {
        return new CurrencyResponseDto
        {
            Code = firsCurrency.Code,
            Name = firsCurrency.Name,
            Symbol = firsCurrency.Symbol,
            SymbolNative = firsCurrency.SymbolNative,
            DecimalDigits = firsCurrency.DecimalDigits,
            Rounding = firsCurrency.Rounding,
            NamePlural = firsCurrency.NamePlural
        };
    }

    /// <summary>
    /// Maps FIRS InvoiceType to Unified InvoiceTypeResponseDto
    /// </summary>
    public static InvoiceTypeResponseDto MapInvoiceType(FirsResponseInvoiceType firsInvoiceType)
    {
        return new InvoiceTypeResponseDto
        {
            Code = firsInvoiceType.Code,
            Name = firsInvoiceType.Value,
            Value = firsInvoiceType.Value
        };
    }

    /// <summary>
    /// Maps FIRS PaymentMeans to Unified PaymentMeansResponseDto
    /// </summary>
    public static PaymentMeansResponseDto MapPaymentMeans(FirsResponsePaymentMeans firsPaymentMeans)
    {
        return new PaymentMeansResponseDto
        {
            Code = firsPaymentMeans.Code,
            Name = firsPaymentMeans.Value,
            Value = firsPaymentMeans.Value
        };
    }

    /// <summary>
    /// Maps FIRS TaxCategory to Unified TaxCategoryResponseDto
    /// </summary>
    public static TaxCategoryResponseDto MapTaxCategory(FirsResponseTaxCategory firsTaxCategory)
    {
        return new TaxCategoryResponseDto
        {
            Code = firsTaxCategory.Code,
            Value = firsTaxCategory.Value,
            Percent = firsTaxCategory.Percent
        };
    }

    /// <summary>
    /// Maps FIRS Country to Unified CountryResponseDto
    /// </summary>
    public static CountryResponseDto MapCountry(FirsResponseCountry firsCountry)
    {
        return new CountryResponseDto
        {
            Name = firsCountry.Name,
            Alpha2 = firsCountry.Alpha2,
            Alpha3 = firsCountry.Alpha3,
            CountryCode = firsCountry.CountryCode,
            Iso31662 = firsCountry.Iso31662,
            Region = firsCountry.Region,
            SubRegion = firsCountry.SubRegion,
            IntermediateRegion = firsCountry.IntermediateRegion,
            RegionCode = firsCountry.RegionCode,
            SubRegionCode = firsCountry.SubRegionCode,
            IntermediateRegionCode = firsCountry.IntermediateRegionCode
        };
    }

    /// <summary>
    /// Maps FIRS ServiceCode to Unified ServiceCodeResponseDto
    /// </summary>
    public static ServiceCodeResponseDto MapServiceCode(FirsResponseServiceCode firsServiceCode)
    {
        return new ServiceCodeResponseDto
        {
            Code = firsServiceCode.Code,
            Description = firsServiceCode.Description
        };
    }

    /// <summary>
    /// Maps FIRS VatExemption to Unified VatExemptionResponseDto
    /// </summary>
    public static VatExemptionResponseDto MapVatExemption(FirsResponseVatExemption firsVatExemption)
    {
        return new VatExemptionResponseDto
        {
            HeadingNo = firsVatExemption.HeadingNo,
            HarmonizedSystemCode = firsVatExemption.HarmonizedSystemCode,
            TariffCategory = firsVatExemption.TariffCategory,
            Tariff = firsVatExemption.Tariff,
            Description = firsVatExemption.Description
        };
    }
}