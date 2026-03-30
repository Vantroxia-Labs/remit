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
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation(
    Summary = "Upsert Business FlowRule (Merchant Admin)",
    Description = @"
Create business flow rule using range-based amount matching with automatic approval routing.

**How It Works:**
- Invoices are matched to FlowRules based on their total amount
- If `requiresClientAdminApproval` is **false**: Invoice is **auto-approved** (status = APPROVED)
- If `requiresClientAdminApproval` is **true**: Invoice requires approval (status = PENDING_APPROVAL)
- When multiple rules match, the one with lowest priority number (highest priority) is used

**Example 1: Auto-Approve Small Invoices**
```json
{
  ""name"": ""Auto-Approve Small Invoices"",
  ""description"": ""Automatically approve invoices up to 200,000"",
  ""minAmount"": 0,
  ""maxAmount"": 200000,
  ""requiresClientAdminApproval"": false,
  ""priority"": 1,
  ""enableTimeBasedRules"": false
}
```

**Example 2: Require Approval for Large Invoices**
```json
{
  ""name"": ""Require Approval for Large Invoices"",
  ""description"": ""Invoices above 200,000 require ClientAdmin approval"",
  ""minAmount"": 200001,
  ""maxAmount"": 9999999999999999.99,
  ""requiresClientAdminApproval"": true,
  ""priority"": 1,
  ""enableTimeBasedRules"": false
}
```

**Example 3: Time-Based Auto-Approval (Business Hours Only)**
```json
{
  ""name"": ""Business Hours Auto-Approval"",
  ""description"": ""Auto-approve small invoices during business hours only"",
  ""minAmount"": 0,
  ""maxAmount"": 100000,
  ""requiresClientAdminApproval"": false,
  ""priority"": 1,
  ""enableTimeBasedRules"": true,
  ""activeStartTime"": ""09:00:00"",
  ""activeEndTime"": ""17:00:00"",
  ""activeDaysOfWeek"": [1, 2, 3, 4, 5]
}
```

**Invoice Workflow:**
1. **Invoice Created** → Status = CREATED
2. **FlowRule Matched** based on invoice amount
3. **Auto-Approve Path** (`requiresClientAdminApproval: false`) → Status = APPROVED
4. **Manual Approval Path** (`requiresClientAdminApproval: true`) → Status = PENDING_APPROVAL
5. **Admin Approves** → Status = APPROVED
6. **Admin Rejects** → Status = REJECTED

**Key Points:**
- **Amount Ranges:** Invoices matched based on `minAmount` and `maxAmount` (inclusive)
- **Priority:** Lower number = higher priority when multiple rules match
- **Complete Coverage:** Ensure FlowRules cover all invoice amounts with no gaps
- **Time-Based Rules:** Optional - restricts rule activation to specific hours/days
- **Days of Week:** 0=Sunday, 1=Monday, 2=Tuesday, 3=Wednesday, 4=Thursday, 5=Friday, 6=Saturday
"
)]

    [SwaggerResponse(200, "Create successful", typeof(ApiResponse<CreateFlowRuleResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<CreateFlowRuleResponse>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<CreateFlowRuleResponse>))]
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
    [SwaggerOperation(
        Summary = "Soft Delete Business FlowRule (Merchant Admin)",
        Description = @"Soft delete a business flow rule. This operation marks the flow rule as deleted without permanently removing it from the database.

**Key Points:**
- Only the FlowRule owner (business admin) can delete their own FlowRules
- This is a soft delete operation - the FlowRule is marked as deleted but not physically removed
- Deleted FlowRules will not appear in listing or detail queries
- The operation is irreversible through the API (requires database-level intervention to restore)

**Security:**
- Only ClientAdmin role can perform this operation
- Users can only delete FlowRules from their own business
- Authentication and business context validation is enforced"
    )]
    [SwaggerResponse(200, "Delete successful", typeof(ApiResponse<SoftDeleteFlowRuleResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<SoftDeleteFlowRuleResponse>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<SoftDeleteFlowRuleResponse>))]
    [SwaggerResponse(404, "FlowRule not found", typeof(ApiResponse<SoftDeleteFlowRuleResponse>))]
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
    [SwaggerOperation(
        Summary = "Get FlowRule Details for Current Business (Merchant Admin)",
        Description = @"Get flowrule details for the current user's business. Returns comprehensive workflow information including:

- **Basic FlowRule Information**: Name, description, amount, creation/update dates
- **Actions and Next Steps**: Traditional actions and next steps for the flow rule
- **Action-NextStep Mappings**: Detailed mappings showing which next step is taken for each action
- **Admin Approval Workflow**: Complete admin approval workflow information if applicable
- **Workflow Analysis**: Workflow type description, completeness indicators, and business logic warnings

For admin approval workflows, the response will include:
- ApprovalActionMappings showing Approve/Disapprove action flows
- AdminApprovalWorkflow showing the complete approval process
- IsCompleteAdminApprovalWorkflow indicating if the workflow has both approve and disapprove options

Uses the current user's BusinessId to ensure users can only see their own business flowrules."
    )]
    [SwaggerResponse(200, "Request successful", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
    [SwaggerResponse(404, "No flowrules found", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
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
    [SwaggerOperation(
        Summary = "Get All FlowRules",
        Description = @"Retrieve all FlowRules with pagination, search, and sorting capabilities.

**Access Control:**
- **System Admin**: Can see all FlowRules from all businesses
- **Merchant Admin**: Can only see FlowRules from their own business

**Features:**
- **Pagination**: Use pageNumber and pageSize parameters
- **Search**: Filter by name or description using searchTerm
- **Sorting**: Sort by name, description, amount, createdat, or updatedat
- **Security**: Role-based access with business isolation

**Query Parameters:**
- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 10)
- `searchTerm`: Search in name and description
- `sortBy`: Field to sort by (name, description, amount, createdat, updatedat)
- `sortDescending`: Sort order (default: false - ascending)

**Response Format:**
Returns simplified FlowRule format with essential information and action mappings."
    )]
    [SwaggerResponse(200, "Request successful", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
    [SwaggerResponse(404, "No flowrules found", typeof(ApiResponse<IEnumerable<FlowRuleDetailsResponseDto>>))]
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
