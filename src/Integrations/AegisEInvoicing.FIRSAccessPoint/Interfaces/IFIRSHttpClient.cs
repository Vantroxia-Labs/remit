using AegisEInvoicing.FIRSAccessPoint.Attributes;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.Authentication;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ReportInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.SignInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.UpdateInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateIRN;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.Authentication;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ConfirmInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.DownloadInvoice;
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
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ReportInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.SignInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.UpdateInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ValidateInvoiceData;
using AegisEInvoicing.FIRSAccessPoint.Models.Responses.ValidateIRN;

namespace AegisEInvoicing.FIRSAccessPoint.Interfaces;

/// <summary>
/// HTTP client interface for FIRS (Federal Inland Revenue Service) integration.
/// This service is tenant-agnostic as it provides shared functionality across all tenants.
/// </summary>
[TenantAgnostic("FIRS integration is a shared service that operates independently of tenant boundaries")]
public interface IFIRSHttpClient
{
    #region Authentication
    Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    Task<bool> ValidateConnectionAsync(string apiKey, string apiSecret, CancellationToken cancellationToken = default);
    #endregion

    #region AccessPointProviders

    Task<ValidateInvoiceDataResponse> ValidateInvoiceDataAsync(ValidateInvoiceDataRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    Task<ValidateIrnResponse> ValidateIrnAsync(ValidateIrnRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    Task<SignInvoiceResponse> SignInvoiceAsync(SignInvoiceRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    Task<ReportInvoiceResponse> ReportInvoiceAsync(ReportInvoiceRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    Task<UpdateInvoiceResponse> UpdateInvoiceAsync(string irn, UpdateInvoiceRequest request, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    Task<ConfirmInvoiceResponse> ConfirmInvoiceAsync(string irn, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    Task<DownloadInvoiceResponse> DownloadInvoiceAsync(string irn, string apiKey, string apiSecret, CancellationToken cancellationToken = default);

    #endregion

    #region Resources

    Task<GetInvoiceTypeResponse> GetInvoiceType(CancellationToken cancellationToken = default);

    Task<GetPaymentMeansResponse> GetPaymentMeans(CancellationToken cancellationToken = default);

    Task<GetTaxCategoriesResponse> GetTaxCategories(CancellationToken cancellationToken = default);

    Task<GetCurrenciesResponse> GetCurrencies(CancellationToken cancellationToken = default);

    Task<GetVatExemptionsResponse> GetVatExemptions(CancellationToken cancellationToken = default);

    Task<GetProductsCodesResponse> GetProductsCodes(CancellationToken cancellationToken = default);

    Task<GetServiceCodesResponse> GetServiceCodes(CancellationToken cancellationToken = default);

    Task<GetAllLocalGovernmentsResponse> GetAllLocalGovernments(CancellationToken cancellationToken = default);

    Task<GetAllStatesResponse> GetAllStates(CancellationToken cancellationToken = default);

    Task<GetAllCountriesResponse> GetAllCountries(CancellationToken cancellationToken = default);

    #endregion
}