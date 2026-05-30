using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Business.Request;
using AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Response;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.OnboardBusiness;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class BusinessController
{
    /// <summary>
    /// Onboard business to EInvoice Integrator platform (KMPG Platform Admin only)
    /// </summary>
    /// <param name="request">Business onboarding details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Onboarding result with subscription status</returns>
    [HttpPost("onboard-business")]    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> OnboardBusinessAsync([FromBody] OnboardBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("KMPG business onboarding to EInvoice Integrator platform requested for TIN: {TIN}, Registration Number: {RegNumber}",
            request.TIN, request.BusinessRegistrationNumber);

        var address = Address.Create(
            request.RegisteredAddress.Street,
            request.RegisteredAddress.City,
            request.RegisteredAddress.State,
            request.RegisteredAddress.Country,
            request.RegisteredAddress.PostalCode);

        var command = new OnboardBusinessCommand(
            request.BusinessName,
            request.TIN,
            request.BusinessRegistrationNumber,
            address,
            "INV",
            request.FIRSBusinessId,
            request.Industry,
            request.ContactEmail,
            request.ContactPhone,
            request.Description,
            request.ServiceId,
            request.AdminFirstName,
            request.AdminLastName,
            request.Subscription.PlatformSubscriptionId,
            request.Subscription.Duration,
            request.Subscription.SubscriptionStartDate,
            request.DeploymentMode);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to onboard business to platform: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("KMPG successfully onboarded business to EInvoice Integrator platform. BusinessId: {BusinessId}", result.BusinessId);

        var response = new OnboardBusinessResponse
        {
            BusinessId = result.BusinessId!.Value,
            ConnectionStatus = result.ConnectionStatus!,
            Message = result.Message
        };

        return Success(response, "Business successfully onboarded to EInvoice Integrator platform");
    }
}