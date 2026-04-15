using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Paystack.Interfaces;
using AegisEInvoicing.Paystack.Models.Requests;
using AegisEInvoicing.Paystack.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Manages Paystack subscription plans and subscriptions - creating, listing, fetching, enabling, and disabling subscriptions and plans for Admins only.
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SubscriptionController(
    IPaystackService paystackService,
    ILogger<SubscriptionController> logger) : BaseApiController
{
    // ============ Subscription Plan Management ============

    /// <summary>
    /// Create a new subscription plan
    /// </summary>
    [HttpPost("plans")]    [ProducesResponseType(typeof(ApiResponse<PlanData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating subscription plan: {Name}", request.Name);

        var result = await paystackService.CreatePlanAsync(request, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(result.Data, "Subscription plan created successfully.");
    }

    /// <summary>
    /// List all subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<List<PlanData>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPlans(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching subscription plans. Page: {Page}, PerPage: {PerPage}", page, perPage);

        var result = await paystackService.ListPlansAsync(page, perPage, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(result.Data, "Subscription plans retrieved successfully.");
    }

    /// <summary>
    /// Get a specific subscription plan
    /// </summary>
    [HttpGet("plans/{idOrCode}")]
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<PlanData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlan(
        string idOrCode,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching subscription plan: {IdOrCode}", idOrCode);

        var result = await paystackService.FetchPlanAsync(idOrCode, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(result.Data, "Subscription plan retrieved successfully.");
    }

    /// <summary>
    /// Update a subscription plan
    /// </summary>
    [HttpPut("plans/{idOrCode}")]    [ProducesResponseType(typeof(ApiResponse<PlanData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePlan(
        string idOrCode,
        [FromBody] CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating subscription plan: {IdOrCode}", idOrCode);

        var result = await paystackService.UpdatePlanAsync(idOrCode, request, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(result.Data, "Subscription plan updated successfully.");
    }

    // ============ Subscription Management ============

    /// <summary>
    /// Create a new subscription for a customer
    /// </summary>
    [HttpPost("subscriptions")]    [ProducesResponseType(typeof(ApiResponse<SubscriptionData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating subscription for customer: {Customer}, plan: {Plan}",
            request.Customer, request.Plan);

        var result = await paystackService.CreateSubscriptionAsync(request, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(result.Data, "Subscription created successfully.");
    }

    /// <summary>
    /// List all subscriptions
    /// </summary>
    [HttpGet("subscriptions")]    [ProducesResponseType(typeof(ApiResponse<List<SubscriptionData>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching subscriptions. Page: {Page}, PerPage: {PerPage}", page, perPage);

        var result = await paystackService.ListSubscriptionsAsync(page, perPage, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(result.Data, "Subscriptions retrieved successfully.");
    }

    /// <summary>
    /// Get a specific subscription
    /// </summary>
    [HttpGet("subscriptions/{idOrCode}")]    [ProducesResponseType(typeof(ApiResponse<SubscriptionData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(
        string idOrCode,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching subscription: {IdOrCode}", idOrCode);

        var result = await paystackService.FetchSubscriptionAsync(idOrCode, cancellationToken);

        if (!result.Status || result.Data is null)
            return Error(result.Message);

        return Success(result.Data, "Subscription retrieved successfully.");
    }

    /// <summary>
    /// Enable a subscription
    /// </summary>
    [HttpPost("subscriptions/{code}/enable")]    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableSubscription(
        string code,
        [FromBody] SubscriptionToggleRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Enabling subscription: {Code}", code);

        var result = await paystackService.EnableSubscriptionAsync(code, request.EmailToken, cancellationToken);

        if (!result.Status)
            return Error(result.Message);

        return Success(result.Data, "Subscription enabled successfully.");
    }

    /// <summary>
    /// Disable a subscription
    /// </summary>
    [HttpPost("subscriptions/{code}/disable")]    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableSubscription(
        string code,
        [FromBody] SubscriptionToggleRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Disabling subscription: {Code}", code);

        var result = await paystackService.DisableSubscriptionAsync(code, request.EmailToken, cancellationToken);

        if (!result.Status)
            return Error(result.Message);

        return Success(result.Data, "Subscription disabled successfully.");
    }
}

public record SubscriptionToggleRequest(string EmailToken);
