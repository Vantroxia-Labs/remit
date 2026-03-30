using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ImportFirsInvoices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.FirsMbs;

/// <summary>
/// HTTP client for the FIRS MBS portal.
/// Deserialises raw JSON responses and maps them to clean Application-layer DTOs.
/// Base URL is read from MBS:APIURL in configuration (env var MBS__APIURL).
/// </summary>
public sealed class FirsMbsApiClient : IFirsMbsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FirsMbsApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FirsMbsApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<FirsMbsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["MBS:APIURL"]
                      ?? throw new InvalidOperationException("MBS:APIURL is not configured. Add MBS__APIURL to the .env file.");

        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging in to FIRS MBS portal as {Email}", email);

        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1/client/auth/login/request",
            new FirsMbsLoginRequest { Email = email, Password = password },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"FIRS MBS login failed ({response.StatusCode}): {body}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<FirsMbsApiResponse<FirsMbsLoginData>>(JsonOptions, cancellationToken);

        if (result?.Status != true || string.IsNullOrWhiteSpace(result.Data?.EInvoicingToken))
            throw new InvalidOperationException($"FIRS MBS login returned no token: {result?.Message}");

        _logger.LogInformation("Authenticated with FIRS MBS portal");
        return result.Data.EInvoicingToken;
    }

    public async Task<MbsInvoiceListData?> GetInvoicePageAsync(
        string bearerToken, int page, int size, CancellationToken cancellationToken)
    {
        SetBearer(bearerToken);

        var url = $"/api/v1/client/reports?size={size}&page={page}&sort_by=created_at&sort_direction_desc=true" +
                  "&irn=&payment_status=&entry_status=&invoice_type_code=&issue_date=&due_date&tax_currency_code&document_currency_code";

        _logger.LogInformation("Fetching invoice list page {Page} (size={Size})", page, size);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch invoice list page {Page}: {Status}", page, response.StatusCode);
            return null;
        }

        var raw = await response.Content
            .ReadFromJsonAsync<FirsMbsApiResponse<FirsMbsInvoiceListData>>(JsonOptions, cancellationToken);

        return raw?.Data is null ? null : ToListDto(raw.Data);
    }

    public async Task<MbsInvoiceDetail?> GetInvoiceDetailAsync(
        string bearerToken, string irn, CancellationToken cancellationToken)
    {
        SetBearer(bearerToken);

        var url = $"/api/v1/client/reports/download/{Uri.EscapeDataString(irn)}";
        _logger.LogInformation("Fetching invoice detail for IRN {IRN}", irn);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch detail for IRN {IRN}: {Status}", irn, response.StatusCode);
            return null;
        }

        var raw = await response.Content
            .ReadFromJsonAsync<FirsMbsApiResponse<FirsMbsInvoiceDetail>>(JsonOptions, cancellationToken);

        return raw?.Data is null ? null : ToDetailDto(raw.Data);
    }

    // Application-layer DTO mapping

    private static MbsInvoiceListData ToListDto(FirsMbsInvoiceListData raw) => new(
        raw.Items.Select(i => new MbsInvoiceListItem(
            i.Irn, i.EntryStatus, i.InvoiceTypeCode,
            i.IssueDate, i.IssueTime, i.DueDate, i.DocumentCurrencyCode)).ToList(),
        new MbsPageInfo(raw.Page.Page, raw.Page.Size, raw.Page.HasNextPage, raw.Page.TotalCount));

    private static MbsInvoiceDetail ToDetailDto(FirsMbsInvoiceDetail raw) => new(
        Irn:                  raw.Irn,
        InvoiceTypeCode:      raw.InvoiceTypeCode,
        IssueDate:            raw.IssueDate,
        IssueTime:            raw.IssueTime,
        DueDate:              raw.DueDate,
        Note:                 raw.Note,
        DocumentCurrencyCode: raw.DocumentCurrencyCode,
        DeliveryPeriod:       raw.DeliveryPeriod is null ? null
                                  : new MbsDeliveryPeriod(raw.DeliveryPeriod.StartDate, raw.DeliveryPeriod.EndDate),
        SupplierParty:        ToPartyDto(raw.SupplierParty),
        CustomerParty:        ToPartyDto(raw.CustomerParty),
        PaymentMeans:         raw.PaymentMeans
                                  .Select(p => new MbsPaymentMeans(p.PaymentMeansCode)).ToList(),
        PaymentTermsNote:     raw.PaymentTermsNote,
        TaxTotal:             raw.TaxTotal.Select(t => new MbsTaxTotal(
                                  t.TaxAmount,
                                  t.TaxSubtotal.Select(s => new MbsTaxSubtotal(
                                      s.TaxableAmount, s.TaxAmount,
                                      s.TaxCategory is null ? null
                                          : new MbsTaxCategory(s.TaxCategory.Id, s.TaxCategory.Percent)
                                  )).ToList())).ToList(),
        InvoiceLine:          raw.InvoiceLine.Select(l => new MbsInvoiceLine(
                                  l.InvoicedQuantity, l.LineExtensionAmount,
                                  l.Item is null ? null : new MbsItem(l.Item.Name, l.Item.Description),
                                  l.Price is null ? null : new MbsPrice(l.Price.PriceAmount, l.Price.BaseQuantity),
                                  l.HsnCode, l.ProductCategory, l.ServiceCategory,
                                  l.DiscountRate, l.DiscountAmount, l.FeeRate, l.FeeAmount)).ToList());

    private static MbsParty? ToPartyDto(FirsMbsParty? raw) =>
        raw is null ? null : new MbsParty(
            raw.PartyName, raw.Tin, raw.Email, raw.Telephone, raw.BusinessDescription,
            raw.PostalAddress is null ? null : new MbsPostalAddress(
                raw.PostalAddress.StreetName, raw.PostalAddress.CityName,
                raw.PostalAddress.PostalZone, raw.PostalAddress.State, raw.PostalAddress.Country));

    private void SetBearer(string token) =>
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
}
