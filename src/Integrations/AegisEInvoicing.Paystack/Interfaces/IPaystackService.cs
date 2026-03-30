using AegisEInvoicing.Paystack.Models.Requests;
using AegisEInvoicing.Paystack.Models.Responses;

namespace AegisEInvoicing.Paystack.Interfaces;

public interface IPaystackService
{
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

    /// <summary>
    /// Validates a Paystack webhook signature
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature);

    /// <summary>
    /// Generates a unique payment reference
    /// </summary>
    string GenerateReference(string prefix = "AEGIS");
}
