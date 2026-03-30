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
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.ERP.API.Controllers;

public partial class FIRSController
{

    /// <summary>
    /// Get Tax Categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tax Categories</returns>
    [HttpGet("get-taxcategories")]
    [SwaggerOperation(
        Summary = "Get Tax Categories",
        Description = "Retrieves all tax categories configured in the FIRS (Federal Inland Revenue Service) system. Tax categories are used to classify different types of taxes applicable to invoices.",
        OperationId = "GetTaxCategories",
        Tags = new[] { "FIRS Integration Operations" }
    )]
    [SwaggerResponse(200, "Tax Categories retrieved successfully", typeof(ApiResponse<GetTaxCategoriesResponse>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "Service unavailable - FIRS system is unavailable", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get All Countries",
        Description = "Retrieves a comprehensive list of all countries registered in the FIRS system. This data is used for international transactions and cross-border invoice processing.",
        OperationId = "GetAllCountries",
        Tags = new[] { "FIRS Integration Operations" }
    )]
    [SwaggerResponse(200, "Countries retrieved successfully", typeof(ApiResponse<GetAllCountriesResponse>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "Service unavailable - FIRS system is unavailable", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get All Currencies",
        Description = "Retrieves all supported currencies configured in the FIRS system. Currency codes and information are essential for multi-currency invoice transactions and financial reporting.",
        OperationId = "GetAllCurrencies",
        Tags = new[] { "FIRS Integration Operations" }
    )]
    [SwaggerResponse(200, "Currencies retrieved successfully", typeof(ApiResponse<GetCurrenciesResponse>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "Service unavailable - FIRS system is unavailable", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get Payment Means",
        Description = "Retrieves all available payment methods registered in the FIRS system. Payment means include cash, bank transfer, credit card, and other payment instruments used for invoice settlement.",
        OperationId = "GetPaymentMeans",
        Tags = new[] { "FIRS Integration Operations" }
    )]
    [SwaggerResponse(200, "Payment Means retrieved successfully", typeof(ApiResponse<GetPaymentMeansResponse>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "Service unavailable - FIRS system is unavailable", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get Invoice Types",
        Description = "Retrieves all invoice types configured in the FIRS system. Invoice types classify documents such as standard invoices, credit notes, debit notes, and other transaction types.",
        OperationId = "GetInvoiceTypes",
        Tags = new[] { "FIRS Integration Operations" }
    )]
    [SwaggerResponse(200, "Invoice Types retrieved successfully", typeof(ApiResponse<GetInvoiceTypeResponse>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "Service unavailable - FIRS system is unavailable", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get Service Codes",
        Description = "Retrieves all service classification codes available in the FIRS system. Service codes are used to categorize services provided and ensure proper tax classification for service-based transactions.",
        OperationId = "GetServiceCodes",
        Tags = new[] { "FIRS Integration Operations" }
    )]
    [SwaggerResponse(200, "Service Codes retrieved successfully", typeof(ApiResponse<GetServiceCodesResponse>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "Service unavailable - FIRS system is unavailable", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get VAT Exemptions",
        Description = "Retrieves all VAT (Value Added Tax) exemption categories configured in the FIRS system. VAT exemptions define specific goods, services, or entities that are exempt from standard VAT taxation.",
        OperationId = "GetVatExemptions",
        Tags = new[] { "FIRS Integration Operations" }
    )]
    [SwaggerResponse(200, "VAT Exemptions retrieved successfully", typeof(ApiResponse<GetVatExemptionsResponse>))]
    [SwaggerResponse(400, "Bad request - Invalid parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [SwaggerResponse(503, "Service unavailable - FIRS system is unavailable", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetVatExemptions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VAT exemptions requested");
        var vatExemptions = await _firsClient.GetVatExemptions(cancellationToken);
        return Success(vatExemptions.Data, "VAT Exemptions");
    }
}