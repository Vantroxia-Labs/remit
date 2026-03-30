using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ApproveInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.RejectInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetPendingApprovalInvoices;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class InvoiceController
{
    /// <summary>
    /// Get all invoices pending ClientAdmin approval
    /// </summary>
    [HttpGet("pending-approval")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Get Pending Approval Invoices (ClientAdmin)",
        Description = @"
Retrieves all invoices that are pending ClientAdmin approval for the current business.

**Access:** ClientAdmin only

**How It Works:**
- Only returns invoices with status PENDING_APPROVAL
- Filtered to the ClientAdmin's business
- Supports pagination, date filtering, and search

**Use Cases:**
- ClientAdmin reviews invoices requiring approval
- Dashboard showing pending approvals count
- Approval queue management
        "
    )]
    [SwaggerResponse(200, "Pending approval invoices retrieved successfully")]
    [SwaggerResponse(401, "Unauthorized - ClientAdmin role required")]
    public async Task<IActionResult> GetPendingApprovalInvoices(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] string? orderBy = "createdat",
        [FromQuery] bool orderByDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingApprovalInvoicesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            StartDate = startDate,
            EndDate = endDate,
            OrderBy = orderBy,
            OrderByDescending = orderByDescending
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Success(result, $"Retrieved {result.Items.Count} pending approval invoices");
    }

    /// <summary>
    /// Approve a pending invoice
    /// </summary>
    [HttpPost("{invoiceId:guid}/approve")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Approve Invoice (ClientAdmin)",
        Description = @"
Approves an invoice that is pending ClientAdmin approval.

**Access:** ClientAdmin only

**Requirements:**
- Invoice must be in PENDING_APPROVAL status
- Invoice must belong to the ClientAdmin's business

**What Happens:**
1. Invoice status changes from PENDING_APPROVAL to APPROVED
2. Approval history entry is created with ClientAdmin's name and optional comments
3. Invoice can now proceed to validation and submission to FIRS

**Example Request:**
```json
{
  ""approvalComments"": ""Verified invoice details, approved for submission""
}
```
        "
    )]
    [SwaggerResponse(200, "Invoice approved successfully")]
    [SwaggerResponse(400, "Bad request - Invoice not in PENDING_APPROVAL status")]
    [SwaggerResponse(401, "Unauthorized - ClientAdmin role required")]
    [SwaggerResponse(403, "Forbidden - User not authorized to approve this invoice")]
    [SwaggerResponse(404, "Invoice not found")]
    public async Task<IActionResult> ApproveInvoice(
        [FromRoute] Guid invoiceId,
        [FromBody] ApproveInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ApproveInvoiceCommand(invoiceId, request.ApprovalComments);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapResultToHttpResponse(result.StatusCodes, result.Message);
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// Reject a pending invoice
    /// </summary>
    [HttpPost("{invoiceId:guid}/reject")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Reject Invoice (ClientAdmin)",
        Description = @"
Rejects an invoice that is pending ClientAdmin approval.

**Access:** ClientAdmin only

**Requirements:**
- Invoice must be in PENDING_APPROVAL status
- Invoice must belong to the ClientAdmin's business
- Rejection reason is REQUIRED

**What Happens:**
1. Invoice status changes from PENDING_APPROVAL to REJECTED
2. Rejection history entry is created with ClientAdmin's name and rejection reason
3. Invoice cannot proceed to FIRS submission

**Example Request:**
```json
{
  ""rejectionReason"": ""Invoice amount exceeds approved budget. Please revise and resubmit.""
}
```
        "
    )]
    [SwaggerResponse(200, "Invoice rejected successfully")]
    [SwaggerResponse(400, "Bad request - Invoice not in PENDING_APPROVAL status or missing rejection reason")]
    [SwaggerResponse(401, "Unauthorized - ClientAdmin role required")]
    [SwaggerResponse(403, "Forbidden - User not authorized to reject this invoice")]
    [SwaggerResponse(404, "Invoice not found")]
    public async Task<IActionResult> RejectInvoice(
        [FromRoute] Guid invoiceId,
        [FromBody] RejectInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RejectInvoiceCommand(invoiceId, request.RejectionReason);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapResultToHttpResponse(result.StatusCodes, result.Message);
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// Maps result status codes to appropriate HTTP responses
    /// </summary>
    private IActionResult MapResultToHttpResponse(int statusCode, string? message)
    {
        var errorResponse = new ApiResponse<object>
        {
            Success = false,
            Message = message ?? "An error occurred"
        };

        return statusCode switch
        {
            (int)HttpStatusCodes.NotFound => NotFound(errorResponse),
            (int)HttpStatusCodes.Forbidden => StatusCode(StatusCodes.Status403Forbidden, errorResponse),
            (int)HttpStatusCodes.Unauthorized => Unauthorized(errorResponse),
            _ => BadRequest(errorResponse)
        };
    }
}

/// <summary>
/// Request model for approving an invoice
/// </summary>
public class ApproveInvoiceRequest
{
    /// <summary>
    /// Optional comments from the ClientAdmin approver
    /// </summary>
    [SwaggerSchema("Optional comments from the approver")]
    public string? ApprovalComments { get; set; }
}

/// <summary>
/// Request model for rejecting an invoice
/// </summary>
public class RejectInvoiceRequest
{
    /// <summary>
    /// Reason for rejecting the invoice (REQUIRED)
    /// </summary>
    [Required(ErrorMessage = "Rejection reason is required")]
    [MinLength(10, ErrorMessage = "Rejection reason must be at least 10 characters")]
    [SwaggerSchema("Reason for rejecting the invoice (required, min 10 characters)")]
    public string RejectionReason { get; set; } = string.Empty;
}
