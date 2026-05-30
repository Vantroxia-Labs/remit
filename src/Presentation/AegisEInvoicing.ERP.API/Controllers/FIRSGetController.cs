using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllCountries;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetCurrencies;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetInvoiceType;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetPaymentMeans;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetServiceCodes;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetTaxCategories;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetVatExemptions;
using AegisEInvoicing.ERP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.ERP.API.Controllers;

public partial class FIRSController
{

    /// <summary>
    /// Get Tax Categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tax Categories</returns>
    [HttpGet("get-taxcategories")]
    public async Task<IActionResult> GetTaxCategories(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tax Categories requested");

        var taxCategories = await _firsClient.GetTaxCategories(cancellationToken);

        return Success(taxCategories.Data, "Tax Categories" );
    }

    /// <summary>
    /// Get All Countries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of Countries</returns>
    [HttpGet("get-allcountries")]
    public async Task<IActionResult> GetAllCountries(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("All countries requested");
        var countries = await _firsClient.GetAllCountries(cancellationToken);
        return Success(countries.Data, "Countries retrieved successfully");
    }

    /// <summary>
    /// Get All Currencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of Currencies</returns>
    [HttpGet("get-allcurrencies")]
    public async Task<IActionResult> GetAllCurrencies(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("All currencies requested");
        var currencies = await _firsClient.GetCurrencies(cancellationToken);
        return Success(currencies.Data, "Currencies retrieved successfully");
    }

    /// <summary>
    /// Get Payment Means
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment Means</returns>
    [HttpGet("get-paymentmeans")]
    public async Task<IActionResult> GetPaymentMeans(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Payment means requested");
        var paymentMeans = await _firsClient.GetPaymentMeans(cancellationToken);
        return Success(paymentMeans.Data, "Payment Means");
    }

    /// <summary>
    /// Get Invoice Types
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invoice Types</returns>
    [HttpGet("get-invoicetypes")]
    public async Task<IActionResult> GetInvoiceTypes(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invoice types requested");
        var invoiceTypes = await _firsClient.GetInvoiceType(cancellationToken);
        return Success(invoiceTypes.Data, "Invoice Types");
    }

    /// <summary>
    /// Get Service Codes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service Codes</returns>
    [HttpGet("get-servicecodes")]
    public async Task<IActionResult> GetServiceCodes(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Service codes requested");
        var serviceCodes = await _firsClient.GetServiceCodes(cancellationToken);
        return Success(serviceCodes.Data, "Service Codes");
    }

    /// <summary>
    /// Get VAT Exemptions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT Exemptions</returns>
    [HttpGet("get-vatexemptions")]
    public async Task<IActionResult> GetVatExemptions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VAT exemptions requested");
        var vatExemptions = await _firsClient.GetVatExemptions(cancellationToken);
        return Success(vatExemptions.Data, "VAT Exemptions");
    }
}
