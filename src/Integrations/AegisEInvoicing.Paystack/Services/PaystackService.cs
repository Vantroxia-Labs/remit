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

    // ============ Subscription Plan Methods ============

    public async Task<PaystackResponse<PlanData>> CreatePlanAsync(
        CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating Paystack plan: {Name}, amount: {Amount}, interval: {Interval}",
                request.Name, request.Amount, request.Interval);

            var response = await _httpClient.PostAsJsonAsync(
                "/plan",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<PlanData>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<PlanData>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Paystack plan created. Plan Code: {PlanCode}", result.Data?.PlanCode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paystack plan: {Name}", request.Name);
            return new PaystackResponse<PlanData>
            {
                Status = false,
                Message = $"Plan creation failed: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<List<PlanData>>> ListPlansAsync(
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching Paystack plans. Page: {Page}, PerPage: {PerPage}", page, perPage);

            var response = await _httpClient.GetAsync(
                $"/plan?page={page}&perPage={perPage}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<List<PlanData>>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<List<PlanData>>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Fetched {Count} Paystack plans", result.Data?.Count ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Paystack plans");
            return new PaystackResponse<List<PlanData>>
            {
                Status = false,
                Message = $"Failed to fetch plans: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<PlanData>> FetchPlanAsync(
        string idOrCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching Paystack plan: {IdOrCode}", idOrCode);

            var response = await _httpClient.GetAsync(
                $"/plan/{Uri.EscapeDataString(idOrCode)}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<PlanData>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<PlanData>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Fetched Paystack plan: {Name}", result.Data?.Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Paystack plan: {IdOrCode}", idOrCode);
            return new PaystackResponse<PlanData>
            {
                Status = false,
                Message = $"Failed to fetch plan: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<PlanData>> UpdatePlanAsync(
        string idOrCode,
        CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating Paystack plan: {IdOrCode}", idOrCode);

            var response = await _httpClient.PutAsJsonAsync(
                $"/plan/{Uri.EscapeDataString(idOrCode)}",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<PlanData>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<PlanData>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Paystack plan updated: {Name}", result.Data?.Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Paystack plan: {IdOrCode}", idOrCode);
            return new PaystackResponse<PlanData>
            {
                Status = false,
                Message = $"Failed to update plan: {ex.Message}"
            };
        }
    }

    // ============ Subscription Methods ============

    public async Task<PaystackResponse<SubscriptionData>> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating Paystack subscription for customer: {Customer}, plan: {Plan}",
                request.Customer, request.Plan);

            var response = await _httpClient.PostAsJsonAsync(
                "/subscription",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<SubscriptionData>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<SubscriptionData>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Paystack subscription created. Subscription Code: {SubscriptionCode}",
                result.Data?.SubscriptionCode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paystack subscription for customer: {Customer}", request.Customer);
            return new PaystackResponse<SubscriptionData>
            {
                Status = false,
                Message = $"Subscription creation failed: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<List<SubscriptionData>>> ListSubscriptionsAsync(
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching Paystack subscriptions. Page: {Page}, PerPage: {PerPage}",
                page, perPage);

            var response = await _httpClient.GetAsync(
                $"/subscription?page={page}&perPage={perPage}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<List<SubscriptionData>>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<List<SubscriptionData>>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Fetched {Count} Paystack subscriptions", result.Data?.Count ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Paystack subscriptions");
            return new PaystackResponse<List<SubscriptionData>>
            {
                Status = false,
                Message = $"Failed to fetch subscriptions: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<SubscriptionData>> FetchSubscriptionAsync(
        string idOrCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching Paystack subscription: {IdOrCode}", idOrCode);

            var response = await _httpClient.GetAsync(
                $"/subscription/{Uri.EscapeDataString(idOrCode)}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<SubscriptionData>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<SubscriptionData>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Fetched Paystack subscription. Status: {Status}", result.Data?.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Paystack subscription: {IdOrCode}", idOrCode);
            return new PaystackResponse<SubscriptionData>
            {
                Status = false,
                Message = $"Failed to fetch subscription: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<object>> EnableSubscriptionAsync(
        string code,
        string emailToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enabling Paystack subscription: {Code}", code);

            var payload = new { code, token = emailToken };

            var response = await _httpClient.PostAsJsonAsync(
                "/subscription/enable",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<object>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<object>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Paystack subscription enabled: {Code}", code);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable Paystack subscription: {Code}", code);
            return new PaystackResponse<object>
            {
                Status = false,
                Message = $"Failed to enable subscription: {ex.Message}"
            };
        }
    }

    public async Task<PaystackResponse<object>> DisableSubscriptionAsync(
        string code,
        string emailToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disabling Paystack subscription: {Code}", code);

            var payload = new { code, token = emailToken };

            var response = await _httpClient.PostAsJsonAsync(
                "/subscription/disable",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PaystackResponse<object>>(cancellationToken);

            if (result is null)
                return new PaystackResponse<object>
                {
                    Status = false,
                    Message = "Empty response from Paystack"
                };

            _logger.LogInformation("Paystack subscription disabled: {Code}", code);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable Paystack subscription: {Code}", code);
            return new PaystackResponse<object>
            {
                Status = false,
                Message = $"Failed to disable subscription: {ex.Message}"
            };
        }
    }
}
