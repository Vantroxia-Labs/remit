using AegisEInvoicing.Paystack.Models.Requests;
using AegisEInvoicing.Paystack.Models.Responses;

namespace AegisEInvoicing.Paystack.Interfaces;

public interface IPaystackService
{
    // ============ Transaction Methods ============

    /// <summary>
    /// Initializes a Paystack transaction and returns the authorization URL
    /// </summary>
    Task<PaystackResponse<InitializeTransactionData>> InitializeTransactionAsync(
        InitializeTransactionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a Paystack transaction by reference
    /// </summary>
    Task<PaystackResponse<VerifyTransactionData>> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken = default);

    // ============ Subscription Plan Methods ============

    /// <summary>
    /// Creates a new subscription plan
    /// </summary>
    Task<PaystackResponse<PlanData>> CreatePlanAsync(
        CreatePlanRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all subscription plans
    /// </summary>
    Task<PaystackResponse<List<PlanData>>> ListPlansAsync(
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a specific plan by ID or code
    /// </summary>
    Task<PaystackResponse<PlanData>> FetchPlanAsync(
        string idOrCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a subscription plan
    /// </summary>
    Task<PaystackResponse<PlanData>> UpdatePlanAsync(
        string idOrCode,
        CreatePlanRequest request,
        CancellationToken cancellationToken = default);

    // ============ Subscription Methods ============

    /// <summary>
    /// Creates a new subscription
    /// </summary>
    Task<PaystackResponse<SubscriptionData>> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all subscriptions
    /// </summary>
    Task<PaystackResponse<List<SubscriptionData>>> ListSubscriptionsAsync(
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a specific subscription by ID or code
    /// </summary>
    Task<PaystackResponse<SubscriptionData>> FetchSubscriptionAsync(
        string idOrCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a subscription
    /// </summary>
    Task<PaystackResponse<object>> EnableSubscriptionAsync(
        string code,
        string emailToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a subscription
    /// </summary>
    Task<PaystackResponse<object>> DisableSubscriptionAsync(
        string code,
        string emailToken,
        CancellationToken cancellationToken = default);

    // ============ Utility Methods ============

    /// <summary>
    /// Validates a Paystack webhook signature
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature);

    /// <summary>
    /// Generates a unique payment reference
    /// </summary>
    string GenerateReference(string prefix = "AEGIS");
}
