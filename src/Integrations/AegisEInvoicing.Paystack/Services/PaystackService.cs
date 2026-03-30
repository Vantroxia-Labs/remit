using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using AegisEInvoicing.Paystack.Configuration;
using AegisEInvoicing.Paystack.Interfaces;
using AegisEInvoicing.Paystack.Models.Requests;
using AegisEInvoicing.Paystack.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.Paystack.Services;

public class PaystackService(
    HttpClient httpClient,
    IOptions<PaystackOptions> options,
    ILogger<PaystackService> logger) : IPaystackService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly PaystackOptions _options = options.Value;
    private readonly ILogger<PaystackService> _logger = logger;

    public async Task<PaystackResponse<InitializeTransactionData>> InitializeTransactionAsync(
        InitializeTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Paystack transaction for {Email}, amount: {Amount}",
                request.Email, request.Amount);

            var response = await _httpClient.PostAsJsonAsync(
                "/transaction/initialize",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<InitializeTransactionData>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<InitializeTransactionData>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Paystack transaction initialized. Reference: {Reference}",
                result.Data?.Reference);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Paystack transaction for {Email}", request.Email);
            return new PaystackResponse<InitializeTransactionData>
            {
                Status = false,
                Message = $"Payment initialization failed: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<VerifyTransactionData>> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying Paystack transaction. Reference: {Reference}", reference);

            var response = await _httpClient.GetAsync(
                $"/transaction/verify/{Uri.EscapeDataString(reference)}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<VerifyTransactionData>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<VerifyTransactionData>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Paystack transaction verified. Status: {Status}", result.Data?.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Paystack transaction. Reference: {Reference}", reference);
            return new PaystackResponse<VerifyTransactionData>
            {
                Status = false,
                Message = $"Payment verification failed: {ex.Message}"
            };
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            _logger.LogWarning("Paystack webhook secret not configured. Skipping signature validation.");
            return false;
        }

        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(_options.WebhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA512(secretBytes);
            var computedHash = hmac.ComputeHash(payloadBytes);
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            return computedSignature == signature.ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Paystack webhook signature");
            return false;
        }
    }

    public string GenerateReference(string prefix = "AEGIS")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var randomPart = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToUpperInvariant();
        return $"{prefix}-{timestamp}-{randomPart}";
    }
}
