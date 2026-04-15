using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateRegistration;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetSubscriptionPlans;
using AegisEInvoicing.Paystack.Interfaces;
using AegisEInvoicing.Paystack.Models.Requests;
using AegisEInvoicing.Paystack.Models.Webhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Handles Paystack payment webhooks and verification for subscription payments
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentController(
    IMediator mediator,
    IPaystackService paystackService,
    ILogger<PaymentController> logger) : BaseApiController
{
    /// <summary>
    /// Initialize a payment transaction for subscription or other payments
    /// </summary>
    [HttpPost("initialize")]
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<PaymentInitializationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitializePayment(
        [FromBody] PaymentInitializationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reference = string.IsNullOrWhiteSpace(request.Reference) 
            ? paystackService.GenerateReference() 
            : request.Reference;

        var paystackRequest = new InitializeTransactionRequest
        {
            Email = request.Email,
            Amount = request.Amount,
            Currency = request.Currency ?? "NGN",
            Reference = reference,
            CallbackUrl = request.CallbackUrl,
            Metadata = request.Metadata is not null
                ? new PaystackMetadata
                {
                    PendingRegistrationId = request.Metadata.PendingRegistrationId,
                    PlanId = request.Metadata.PlanId,
                    BillingCycle = request.Metadata.BillingCycle,
                    BusinessName = request.Metadata.BusinessName,
                    AdminEmail = request.Metadata.AdminEmail
                }
                : null
        };

        var result = await paystackService.InitializeTransactionAsync(paystackRequest, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(new PaymentInitializationResponse(
            AuthorizationUrl: result.Data.AuthorizationUrl,
            AccessCode: result.Data.AccessCode,
            Reference: result.Data.Reference),
            "Payment initialized successfully. Redirect user to authorization URL.");
    }

    /// <summary>
    /// Paystack webhook — called by Paystack when a payment event occurs.
    /// Activates pending business registrations on charge.success.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken = default)
    {
        // Read raw body for signature validation
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault() ?? string.Empty;

        if (!paystackService.ValidateWebhookSignature(payload, signature))
        {
            logger.LogWarning("Invalid Paystack webhook signature from IP {IP}",
                Request.HttpContext.Connection.RemoteIpAddress);
            return BadRequest(new { message = "Invalid signature" });
        }

        PaystackWebhookEvent? webhookEvent;
        try
        {
            webhookEvent = JsonSerializer.Deserialize<PaystackWebhookEvent>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize Paystack webhook payload");
            return BadRequest(new { message = "Invalid payload" });
        }

        if (webhookEvent is null)
            return Ok(); // Acknowledge unknown events

        logger.LogInformation("Received Paystack webhook event: {Event}", webhookEvent.Event);

        switch (webhookEvent.Event)
        {
            case PaystackEvents.ChargeSuccess:
                await HandleChargeSuccessAsync(webhookEvent, cancellationToken);
                break;

            default:
                logger.LogInformation("Unhandled Paystack event type: {Event}", webhookEvent.Event);
                break;
        }

        // Always return 200 to acknowledge receipt
        return Ok(new { message = "Webhook received" });
    }

    /// <summary>
    /// Manually verify a Paystack payment by reference.
    /// Can be used by the frontend to poll payment status after redirect from Paystack.
    /// </summary>
    [HttpGet("verify/{reference}")]
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<PaymentVerificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPayment(string reference, CancellationToken cancellationToken = default)
    {
        var verifyResult = await paystackService.VerifyTransactionAsync(reference, cancellationToken);

        if (!verifyResult.Status || verifyResult.Data is null)
            return Error(verifyResult.Message);

        var transactionData = verifyResult.Data;

        if (!transactionData.IsSuccessful)
        {
            return Success(new PaymentVerificationResponse(
                Reference: reference,
                Status: transactionData.Status,
                IsSuccessful: false,
                BusinessId: null,
                Message: $"Payment status: {transactionData.Status}"));
        }

        // Activate the registration
        var activateCommand = new ActivateRegistrationCommand(reference, transactionData.PaidAt ?? DateTimeOffset.UtcNow);
        var activateResult = await mediator.Send(activateCommand, cancellationToken);

        if (!activateResult.IsSuccess)
        {
            logger.LogError("Failed to activate registration for reference {Reference}: {Message}", reference, activateResult.Message);
            return Error(activateResult.Message);
        }

        return Success(new PaymentVerificationResponse(
            Reference: reference,
            Status: "success",
            IsSuccessful: true,
            BusinessId: activateResult.BusinessId,
            Message: activateResult.AlreadyActivated
                ? "Your account was already activated. Please sign in."
                : "Payment verified. Your account is now active. Check your email for login credentials."),
            "Payment verified successfully.");
    }

    /// <summary>
    /// Returns available subscription plans with pricing
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SubscriptionPlanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetSubscriptionPlansQuery(), cancellationToken);
        return Success(result, "Subscription plans retrieved successfully.");
    }

    private async Task HandleChargeSuccessAsync(PaystackWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var data = webhookEvent.Data;
        if (data is null) return;

        logger.LogInformation("Processing charge.success for reference: {Reference}", data.Reference);

        var command = new ActivateRegistrationCommand(data.Reference, data.PaidAt ?? DateTimeOffset.UtcNow);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess && !result.AlreadyActivated)
            logger.LogError("Failed to activate registration for Paystack reference {Reference}: {Message}", data.Reference, result.Message);
        else
            logger.LogInformation("Activation result for {Reference}: {Message}", data.Reference, result.Message);
    }
}

public record PaymentVerificationResponse(
    string Reference,
    string Status,
    bool IsSuccessful,
    Guid? BusinessId,
    string Message);

public record PaymentInitializationRequest(
    string Email,
    long Amount, // Amount in kobo (NGN 100 = 10000 kobo)
    string? Currency = "NGN",
    string? Reference = null,
    string? CallbackUrl = null,
    PaymentMetadata? Metadata = null);

public record PaymentMetadata(
    string? PendingRegistrationId = null,
    string? PlanId = null,
    string? BillingCycle = null,
    string? BusinessName = null,
    string? AdminEmail = null);

public record PaymentInitializationResponse(
    string AuthorizationUrl,
    string AccessCode,
    string Reference);
