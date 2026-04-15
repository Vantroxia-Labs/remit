using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.DTOs;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceApprovalHistory;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class InvoiceController
{
    /// <summary>
    /// Get invoice approval history by business admin or Aegis admin
    /// </summary>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of invoice approval history with FIRS status and response message</returns>
    [HttpGet("InvoiceApprovalHistory")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]    public async Task<IActionResult> GetInvoiceApprovalHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("About fetching list of approval history.");
        var query = new GetInvoiceApprovalHistoryQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Success(result, "Invoice approval history retrieved successfully.");
    }
}
