using System.Text.Json;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Interswitch.Interfaces;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithTIN;
using AegisEInvoicing.Interswitch.Models.Requests.TransmitInvoice;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Adapts <see cref="IInterswitchHttpClient"/> to the vendor-agnostic
/// <see cref="IAccessPointProviderClient"/> interface used by <see cref="AppProviderRouter"/>.
///
/// The Infrastructure layer owns this adapter because it bridges the Application abstraction
/// and the Interswitch integration — neither of those layers can depend on each other.
///
/// Credential JSON schema:
/// { "clientId": "...", "clientSecret": "...", "tokenEndpoint": "/Api/SwitchTax/Token" }
/// </summary>
public sealed class InterswitchAppAdapter(
    IInterswitchHttpClient client,
    ILogger<InterswitchAppAdapter> logger) : IAccessPointProviderClient
{
    /// <inheritdoc />
    public string ProviderCode => "interswitch";

    /// <inheritdoc />
    public string DisplayName => "Interswitch";

    /// <inheritdoc />
    public void Configure(string baseUrl, string? credentialsJson)
    {
        string clientId = string.Empty, clientSecret = string.Empty;
        string tokenEndpoint = "/Api/SwitchTax/Token";

        if (credentialsJson is not null)
        {
            using var doc = JsonDocument.Parse(credentialsJson);
            var root = doc.RootElement;
            clientId      = root.TryGetProperty("clientId",      out var cid) ? cid.GetString() ?? string.Empty : string.Empty;
            clientSecret  = root.TryGetProperty("clientSecret",  out var cs)  ? cs.GetString()  ?? string.Empty : string.Empty;
            tokenEndpoint = root.TryGetProperty("tokenEndpoint", out var te)  ? te.GetString()  ?? tokenEndpoint : tokenEndpoint;
        }

        client.Configure(baseUrl, clientId, clientSecret, tokenEndpoint);
    }

    /// <inheritdoc />
    public bool SupportsLookupTin => true;

    /// <inheritdoc />
    public async Task<AppProviderResult> TransmitAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.TransmitInvoiceAsync(
                new TransmitInvoiceRequest { IRN = irn },
                cancellationToken);

            if (response?.Data?.Code == 200)
            {
                return AppProviderResult.Success(rawResponse: JsonSerializer.Serialize(response));
            }

            var errorMessage = response?.Data?.Error?.PublicMessage
                            ?? response?.Data?.Error?.Details
                            ?? "Transmission failed";

            return AppProviderResult.Failure(
                errorCode: $"INTERSWITCH_{response?.Data?.Code ?? 0}",
                errorMessage: errorMessage,
                rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch TransmitInvoice failed for IRN {Irn}", irn);
            return AppProviderResult.Failure("INTERSWITCH_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppLookupTinResult> LookupTinAsync(
        string tin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.LookupWithTINAsync(
                new LookupWithTINRequest { TIN = tin },
                cancellationToken);

            var isUp = response?.Data?.Data?.IsUp ?? false;
            var businessRef = response?.Data?.Data?.BusinessReference;

            return new AppLookupTinResult(
                IsSuccess: true,
                IsUp: isUp,
                BusinessReference: businessRef,
                ErrorMessage: isUp ? null : "TIN is invalid or not enrolled on the NRS portal");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Interswitch LookupTIN failed for TIN {Tin}", tin);
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
                : (false, "Interswitch API did not respond successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Interswitch health check failed");
            return (false, ex.Message);
        }
    }
}
