using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Business.Request;
using AegisEInvoicing.Portal.API.Models.Business.Response;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.BusinessFlowRule;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.SoftDeleteBusinessFlowRule;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetAllFlowRules;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFlowRuleDetails;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class BusinessController
{
    /// <summary>   
    /// Create FlowRule for business
    /// </summary>
    /// <param name="request">Business flowrule create details using simplified action-nextstep mapping format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>create result with FlowRule Id</returns>
    [HttpPost("upsert-flowrule")]
    [RequireRole(RoleConstants.ClientAdmin)]

    public async Task<IActionResult> CreateFlowRule([FromBody] SimpleCreateFlowRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Create FlowRule for Business");

        // Validate the request
        var validationErrors = request.Validate().ToList();
        if (validationErrors.Any())
        {
            var errorMessage = string.Join("; ", validationErrors);
            _logger.LogWarning("FlowRule creation failed validation: {ValidationErrors}", errorMessage);
            return BadRequest(Error(errorMessage));
        }

        // Log the FlowRule being created
        _logger.LogInformation("Creating FlowRule '{Name}' with range {MinAmount:N2} - {MaxAmount:N2}, RequiresApproval: {RequiresApproval}",
            request.Name, request.MinAmount, request.MaxAmount, request.RequiresClientAdminApproval);

        var command = new CreateBusinessFlowRuleCommand(
            request.Name,
            request.Description,
            request.MinAmount,
            request.MaxAmount,
            request.RequiresClientAdminApproval,
            request.Priority,
            request.EnableTimeBasedRules,
            request.ActiveStartTime,
            request.ActiveEndTime,
            request.ActiveDaysOfWeek);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create business flow rule: {Message}", result.Message);
            var errorMessage = result.Message;
            if (result.Errors != null && result.Errors.Count != 0)
                errorMessage += $", {string.Join(", ", result.Errors)}";

            return BadRequest(Error(errorMessage));
        }

        _logger.LogInformation("Business Flow Rule Successfully Created. {Message}", result.Message);

        var response = new CreateFlowRuleResponse
        {
            FlowRuleId = result.FlowRuleId!.Value,
            Message = result.Message
        };

        return Success(response, "Business Flow Rule Successfully Upserted");
    }

    /// <summary>
    /// Soft delete FlowRule for business (Merchant Admin)
    /// </summary>
    /// <param name="flowRuleId">FlowRule Id to be soft deleted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Soft delete result with FlowRule Id</returns>
    [HttpDelete("delete-flowrule/{flowRuleId}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> SoftDeleteFlowRule([FromRoute] Guid flowRuleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Soft delete FlowRule with ID: {FlowRuleId}", flowRuleId);

        var command = new SoftDeleteBusinessFlowRuleCommand(flowRuleId);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to soft delete business flow rule: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Business Flow Rule Successfully Soft Deleted. {Message}", result.Message);

        var response = new SoftDeleteFlowRuleResponse
        {
            FlowRuleId = result.FlowRuleId!.Value,
            Message = result.Message
        };

        return Success(response, "Business Flow Rule Successfully Soft Deleted");
    }

    /// <summary>
    /// Get FlowRule details for the current business
    /// </summary>
    /// <param name="flowRuleId">Optional FlowRule Id to get specific flowrule, if not provided returns all flowrules for the business</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FlowRule details for the current user's business including action-nextstep mappings and admin approval workflow information</returns>
    [HttpGet("flowrule-details")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetFlowRuleDetails([FromQuery] Guid? flowRuleId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get FlowRule details for current business");

        var query = new GetFlowRuleDetailsQuery(flowRuleId);

        var result = await _mediator.Send(query, cancellationToken);

        if (result == null || !result.Any())
            return Error("No flowrules found for this business", 404);

        var message = flowRuleId.HasValue ? "FlowRule details retrieved successfully" : $"Retrieved {result.Count()} flowrules for the business";
        return Success(result, message);
    }

    /// <summary>
    /// Get all FlowRules with pagination and search
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="searchTerm">Optional search term to filter by name or description</param>
    /// <param name="sortBy">Optional sort field (name, description, amount, createdat, updatedat)</param>
    /// <param name="sortDescending">Sort in descending order (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of FlowRules</returns>
    [HttpGet("flowrules")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.AegisAdmin)]
    public async Task<IActionResult> GetAllFlowRules(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get all FlowRules - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}",
            pageNumber, pageSize, searchTerm ?? "None");

        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return BadRequest(Error("Page number must be greater than 0"));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(Error("Page size must be between 1 and 100"));
        }

        var query = new GetAllFlowRulesQuery(pageNumber, pageSize, searchTerm, sortBy, sortDescending);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null || !result.Any())
        {
            return Success(Enumerable.Empty<FlowRuleDetailsResponseDto>(), "No flowrules found");
        }

        var message = string.IsNullOrWhiteSpace(searchTerm)
            ? $"Retrieved {result.Count()} flowrules (Page {pageNumber} of {pageSize} items)"
            : $"Found {result.Count()} flowrules matching '{searchTerm}' (Page {pageNumber} of {pageSize} items)";

        return Success(result, message);
    }
}
