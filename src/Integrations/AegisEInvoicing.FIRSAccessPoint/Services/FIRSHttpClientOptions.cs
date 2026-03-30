namespace AegisEInvoicing.FIRSAccessPoint.Services;

public sealed class FIRSHttpClientOptions
{
    public const string SectionName = "FIRSHttpClient";

    public string BaseUrl { get; set; } = "https://eivc-k6z6d.ondigitalocean.app";

    public string AuthenticationEndpoint { get; set; } = "api/v1/utilities/authenticate";

    public string ValidateInvoiceDataEndpoint { get; set; } = "api/v1/invoice/validate";

    public string ValidateIrnEndpoint { get; set; } = "api/v1/invoice/irn/validate";

    public string SignInvoiceEndpoint { get; set; } = "api/v1/invoice/sign";

    public string ReportInvoiceEndpoint { get; set; } = "api/v1/vat/postpayment";

    public string UpdateInvoiceEndpoint { get; set; } = "api/v1/invoice/update/{0}";

    public string ConfirmInvoiceEndpoint { get; set; } = "api/v1/invoice/confirm/{0}";

    public string DownloadInvoiceEndpoint { get; set; } = "api/v1/invoice/download/{0}";

    public string GetInvoiceType { get; set; } = "api/v1/invoice/resources/invoice-types";

    public string GetPaymentMeans { get; set; } = "api/v1/invoice/resources/payment-means";

    public string GetTaxCategories { get; set; } = "api/v1/invoice/resources/tax-categories";

    public string GetCurrencies { get; set; } = "api/v1/invoice/resources/currencies";

    public string GetVatExemptions { get; set; } = "api/v1/invoice/resources/vat-exemptions";

    public string GetProductsCodes { get; set; } = "api/v1/invoice/resources/hs-codes";

    public string GetServiceCodes { get; set; } = "api/v1/invoice/resources/services-codes";

    public string GetAllLocalGovernments { get; set; } = "api/v1/invoice/resources/lgas";

    public string GetAllStates{ get; set; } = "api/v1/invoice/resources/states";

    public string GetAllCountries { get; set; } = "api/v1/invoice/resources/countries";


    public string ApiVersion { get; set; } = "v1";

    public Dictionary<string, string> DefaultHeaders { get; set; } = new()
    {
        { "Accept", "application/json" },
        { "User-Agent", "AegisEInvoicing/1.0" }
    };

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool EnableRequestLogging { get; set; } = true;

    public bool EnableResponseLogging { get; set; } = true;

    /// <summary>
    /// Indicates that this service operates independently of tenant boundaries.
    /// FIRS integration is a shared service across all tenants.
    /// </summary>
    public bool IsTenantAgnostic { get; set; } = true;
}