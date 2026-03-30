using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllCountries;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllLocalGovernments;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllStates;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetCurrencies;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetInvoiceType;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetPaymentMeans;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetProductsCodes;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetServiceCodes;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetTaxCategories;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetVatExemptions;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.FIRSAccessPoint.Services;

public sealed partial class FIRSHttpClient : IFIRSHttpClient
{
    public async Task<GetInvoiceTypeResponse> GetInvoiceType(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get Invoice Type");

        var endpoint = BuildEndpoint(_options.GetInvoiceType);
        return await _integrationService.GetDataAsync<GetInvoiceTypeResponse>(endpoint, cancellationToken);
    }

    public async Task<GetPaymentMeansResponse> GetPaymentMeans(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get Payment Means");

        var endpoint = BuildEndpoint(_options.GetPaymentMeans);
        return await _integrationService.GetDataAsync<GetPaymentMeansResponse>(endpoint, cancellationToken);
    }

    public async Task<GetTaxCategoriesResponse> GetTaxCategories(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get Tax Categories");

        var endpoint = BuildEndpoint(_options.GetTaxCategories);
        return await _integrationService.GetDataAsync<GetTaxCategoriesResponse>(endpoint, cancellationToken);
    }

    public async Task<GetCurrenciesResponse> GetCurrencies(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get All Currencies");
        var endpoint = BuildEndpoint(_options.GetCurrencies);
        var result = await _integrationService.GetDataAsync<GetCurrenciesResponse>(endpoint, cancellationToken);

        return new GetCurrenciesResponse
        {
            //Data = [.. result.Data.Where(c => c.Code == "NGN")],
            Data = result.Data,
            Code = result.Code,
            Error = result.Error,
            Message = result.Message,
        };
    }

    public async Task<GetVatExemptionsResponse> GetVatExemptions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get Vat Exemptions");

        var endpoint = BuildEndpoint(_options.GetVatExemptions);
        return await _integrationService.GetDataAsync<GetVatExemptionsResponse>(endpoint, cancellationToken);
    }

    public async Task<GetProductsCodesResponse> GetProductsCodes(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get Products Codes");

        var endpoint = BuildEndpoint(_options.GetProductsCodes);
        return await _integrationService.GetDataAsync<GetProductsCodesResponse>(endpoint, cancellationToken);
    }

    public async Task<GetServiceCodesResponse> GetServiceCodes(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get Service Codes");

        var endpoint = BuildEndpoint(_options.GetServiceCodes);
        return await _integrationService.GetDataAsync<GetServiceCodesResponse>(endpoint, cancellationToken);
    }

    public async Task<GetAllLocalGovernmentsResponse> GetAllLocalGovernments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get All Local Governments");

        var endpoint = BuildEndpoint(_options.GetAllLocalGovernments);
        return await _integrationService.GetDataAsync<GetAllLocalGovernmentsResponse>(endpoint, cancellationToken);
    }

    public async Task<GetAllStatesResponse> GetAllStates(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get All States");

        var endpoint = BuildEndpoint(_options.GetAllStates);
        return await _integrationService.GetDataAsync<GetAllStatesResponse>(endpoint, cancellationToken);
    }

    public async Task<GetAllCountriesResponse> GetAllCountries(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get All Countries");
        var endpoint = BuildEndpoint(_options.GetAllCountries);
        var result = await _integrationService.GetDataAsync<GetAllCountriesResponse>(endpoint, cancellationToken);

        return new GetAllCountriesResponse
        {
            //Data = [.. result.Data.Where(c => c.Alpha2 == "NG")],
            Data = result.Data,
            Code = result.Code,
            Error = result.Error,
            Message = result.Message,
        };
    }
}
