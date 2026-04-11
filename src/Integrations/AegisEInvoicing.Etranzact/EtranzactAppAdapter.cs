using System.Text.Json;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Etranzact.Contracts;
using AegisEInvoicing.Etranzact.Models.Requests;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Etranzact;

/// <summary>
/// Adapts <see cref="IEtranzactClient"/> to the vendor-agnostic
/// <see cref="IAccessPointProviderClient"/> interface used by <see cref="AppProviderRouter"/>.
///
/// Credential JSON schema:
/// { "clientApiKey": "...", "clientSecretKey": "..." }
/// </summary>
public sealed class EtranzactAppAdapter(
    IEtranzactClient client,
    ILogger<EtranzactAppAdapter> logger) : IAccessPointProviderClient
{
    /// <inheritdoc />
    public string ProviderCode => "etranzact";

    /// <inheritdoc />
    public string DisplayName => "eTranzact";

    /// <inheritdoc />
    public bool SupportsLookupTin => true;

    /// <inheritdoc />
    public void Configure(string baseUrl, string? credentialsJson)
    {
        string clientApiKey = string.Empty, clientSecretKey = string.Empty;

        if (credentialsJson is not null)
        {
            using var doc = JsonDocument.Parse(credentialsJson);
            var root = doc.RootElement;
            clientApiKey    = root.TryGetProperty("clientApiKey",    out var k) ? k.GetString() ?? string.Empty : string.Empty;
            clientSecretKey = root.TryGetProperty("clientSecretKey", out var s) ? s.GetString() ?? string.Empty : string.Empty;
        }

        client.Configure(baseUrl, clientApiKey, clientSecretKey);

        logger.LogInformation(
            "eTranzact adapter configured with base URL: {BaseUrl}, ApiKey present: {HasKey}",
            baseUrl, !string.IsNullOrWhiteSpace(clientApiKey));
    }

    /// <inheritdoc />
    public async Task<AppProviderResult> TransmitAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.TransmitInvoiceAsync(
                new TransmitInvoiceRequest { Irn = irn }, cancellationToken);

            return response.IsSuccess
                ? AppProviderResult.Success(rawResponse: JsonSerializer.Serialize(response))
                : AppProviderResult.Failure(
                    errorCode: "ETRANZACT_TRANSMIT_ERROR",
                    errorMessage: response.Error ?? response.Message ?? "Transmission failed",
                    rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact TransmitInvoice failed for IRN {Irn}", irn);
            return AppProviderResult.Failure("ETRANZACT_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppLookupTinResult> LookupTinAsync(
        string tin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.VerifyTinAsync(
                new VerifyTinRequest { Tin = tin }, cancellationToken);

            if (!response.IsSuccess)
                return new AppLookupTinResult(false, false, null, response.Error ?? response.Message ?? "TIN lookup failed");

            var isEnrolled = string.Equals(response.Data?.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
            return new AppLookupTinResult(
                IsSuccess: true,
                IsUp: isEnrolled,
                BusinessReference: response.Data?.Id,
                ErrorMessage: isEnrolled ? null : "TIN is inactive or not enrolled on the NRS portal");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eTranzact LookupTIN failed for TIN {Tin}", tin);
            return new AppLookupTinResult(false, false, null, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<(bool IsHealthy, string? Message)> PingAsync(
        CancellationToken cancellationToken = default)
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
            logger.LogError(ex, "eTranzact PingAsync failed");
            return (false, ex.Message);
        }
    }
}
