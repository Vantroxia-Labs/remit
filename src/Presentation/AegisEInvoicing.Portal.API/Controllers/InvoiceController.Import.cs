using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ImportFirsInvoices;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class InvoiceController
{
    /// <summary>
    /// Imports all invoices from the FIRS MBS portal into the local database.
    /// Invoices that already exist (matched by IRN) are skipped.
    /// The FIRS reference-data cache is refreshed before import begins so that
    /// invoice-type names, currency names, and payment-means names resolve to
    /// their live FIRS values.
    /// </summary>
    /// <param name="request">Login credentials for the FIRS MBS portal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import summary with counts of imported, skipped, and failed invoices</returns>
    [HttpPost("import/firs-invoices")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> ImportFirsInvoices(
        [FromBody] FirsImportRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ImportFirsInvoicesResult
            {
                Success = false,
                Message = "Email and password are required."
            });
        }

        _logger.LogInformation("FIRS invoice import requested by {Email}", request.Email);

        var result = await _mediator.Send(
            new ImportFirsInvoicesCommand { Email = request.Email, Password = request.Password },
            cancellationToken);

        _logger.LogInformation(
            "FIRS import finished — Imported: {Imported}, Skipped: {Skipped}, Failed: {Failed}",
            result.TotalImported, result.TotalSkipped, result.TotalFailed);

        return Ok(result);
    }
}

/// <summary>Request model for FIRS MBS portal import.</summary>
public record FirsImportRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
