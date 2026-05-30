using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.CancelSubscription;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinessSubscription;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class BusinessController
{
    /// <summary>   
    /// Get Subscription Tied To a Business
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="businessId">Business Id</param>
    /// <returns>Update result with subscription status</returns>
    /// <returns></returns>
    [HttpGet("get-subscription/{businessId}")]    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetSubscription([FromRoute] Guid? businessId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetch Subscription Tied to a Business.");

        var request = new GetBusinessSubscriptionQuery(businessId);

        var result = await _mediator.Send(request, cancellationToken);

        if (result is null)
            return Error("No Subscription Exists for this business", 404);

        return Success(result, string.Empty);
    }

    [HttpPost("cancel-subscription")]    [ProducesResponseType(typeof(ApiResponse<CancelSubscriptionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionCommand cancel, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancel subscription");

        var result = await _mediator.Send(cancel, cancellationToken);

        if (!result.isSuccess)
            return Error(result.message);

        return Success(result, "Cancel subscription");
    }

}
