using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetVatRemittanceReport;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class InvoiceController
{
    /// <summary>
    /// VAT remittance report — how much output VAT the business has collected on
    /// transmitted invoices and is expected to remit to FIRS for a given year.
    /// </summary>
    /// <param name="startDate">Period start date — defaults to first day of current year</param>
    /// <param name="endDate">Period end date — defaults to today</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("vat-remittance")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> GetVatRemittanceReport(
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var from = startDate ?? new DateOnly(DateTime.UtcNow.Year, 1, 1);
        var to = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (from > to)
            return BadRequest(Error("startDate must be on or before endDate"));
        _logger.LogInformation("VAT remittance report requested: {From} – {To}", from, to);
        var result = await _mediator.Send(new GetVatRemittanceReportQuery(from, to), cancellationToken);
        return Success(result, $"VAT remittance report {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
    }
}
