using System.Text.Json;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.BlueBridge.Contracts;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.BlueBridge;

/// <summary>
/// Adapts <see cref="IBlueBridgeClient"/> to the vendor-agnostic
/// <see cref="IAccessPointProviderClient"/> interface used by <see cref="AppProviderRouter"/>.
///
/// Credential JSON schema:
/// { "apiKey": "..." }
/// </summary>
public sealed class BlueBridgeAppAdapter(
    IBlueBridgeClient client,
    ILogger<BlueBridgeAppAdapter> logger) : IAccessPointProviderClient
{
    /// <inheritdoc />
    public string ProviderCode => "bluebridge";

    /// <inheritdoc />
    public string DisplayName => "BlueBridge";

    /// <inheritdoc />
    public bool SupportsLookupTin => false;

    /// <inheritdoc />
    public void Configure(string baseUrl, string? credentialsJson)
    {
        string apiKey = string.Empty;

        if (credentialsJson is not null)
        {
            using var doc = JsonDocument.Parse(credentialsJson);
            var root = doc.RootElement;
            apiKey = root.TryGetProperty("apiKey", out var k) ? k.GetString() ?? string.Empty : string.Empty;
        }

        client.Configure(baseUrl, apiKey);

        logger.LogInformation(
            "BlueBridge adapter configured with base URL: {BaseUrl}, ApiKey present: {HasKey}",
            baseUrl, !string.IsNullOrWhiteSpace(apiKey));
    }

    /// <inheritdoc />
    public async Task<AppProviderResult> TransmitAsync(
        string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.TransmitInvoiceAsync(irn, cancellationToken);

            return response.IsSuccess
                ? AppProviderResult.Success(rawResponse: JsonSerializer.Serialize(response))
                : AppProviderResult.Failure(
                    errorCode: "BLUEBRIDGE_TRANSMIT_ERROR",
                    errorMessage: response.Message ?? "Transmission failed",
                    rawResponse: JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge TransmitInvoice failed for IRN {Irn}", irn);
            return AppProviderResult.Failure("BLUEBRIDGE_ERROR", ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>BlueBridge does not support TIN enrollment lookup.</remarks>
    public Task<AppLookupTinResult> LookupTinAsync(
        string tin,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AppLookupTinResult(false, false, null, "BlueBridge does not support TIN lookup."));

    /// <inheritdoc />
    public async Task<(bool IsHealthy, string? Message)> PingAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthy = await client.TestConnectionAsync(cancellationToken);
            return healthy
                ? (true, null)
                : (false, "BlueBridge API did not respond successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BlueBridge PingAsync failed");
            return (false, ex.Message);
        }
    }
}
