using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Domain.Constants;
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
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class FIRSController
{

    /// <summary>
    /// Get Tax Categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tax Categories</returns>
    [HttpGet("gettaxcategories")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetTaxCategories(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tax Categories requested");

        var taxCategories = await _firsClient.GetTaxCategories(cancellationToken);

        return Success(taxCategories.Data, "Tax Categories");
    }

    /// <summary>
    /// Get All Countries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of Countries</returns>
    [HttpGet("getallcountries")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetAllCountries(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("All countries requested");
        var countries = await _firsClient.GetAllCountries(cancellationToken);
        return Success(countries.Data, "Countries retrieved successfully");
    }

    /// <summary>
    /// Get All States (from FIRS/NRS)
    /// </summary>
    [HttpGet("getallstates")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetAllStates(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("All states requested");
        var states = await _firsClient.GetAllStates(cancellationToken);
        return Success(states.Data, "States retrieved successfully");
    }

    /// <summary>
    /// Get All Local Governments (LGAs) from FIRS/NRS
    /// </summary>
    [HttpGet("getalllgas")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetAllLgas(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("All LGAs requested");
        var lgas = await _firsClient.GetAllLocalGovernments(cancellationToken);
        return Success(lgas.Data, "LGAs retrieved successfully");
    }

    /// <summary>
    /// Get All Currencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of Currencies</returns>
    [HttpGet("getallcurrencies")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
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
    [HttpGet("getpaymentmeans")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
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
    [HttpGet("getinvoicetypes")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
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
    [HttpGet("getservicecodes")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetServiceCodes(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Service codes requested");
        var serviceCodes = await _firsClient.GetServiceCodes(cancellationToken);
        return Success(serviceCodes.Data, "Service Codes");
    }

    /// <summary>
    /// Get Product Codes (HS Codes for goods)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product Codes</returns>
    [HttpGet("getproductcodes")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetProductCodes(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Product codes requested");
        var productCodes = await _firsClient.GetProductsCodes(cancellationToken);
        return Success(productCodes.Data, "Product Codes");
    }

    /// <summary>
    /// Get VAT Exemptions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT Exemptions</returns>
    [HttpGet("getvatexemptions")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientUser, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetVatExemptions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VAT exemptions requested");
        var vatExemptions = await _firsClient.GetVatExemptions(cancellationToken);
        return Success(vatExemptions.Data, "VAT Exemptions");
    }
}