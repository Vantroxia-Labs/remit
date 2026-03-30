using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.FIRSAccessPoint.Attributes;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for FIRS (Federal Inland Revenue Service) integration operations.
/// This controller is tenant-agnostic as FIRS integration is shared across all tenants.
/// Access is controlled by role-based authorization (Admin, Accountant, Auditor, Viewer).
/// </summary>
[ApiController]
//[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("FIRS Integration Operations")]
[TenantAgnostic("FIRS integration is a shared service used by all tenants")]
public partial class FIRSController(IFIRSHttpClient firsClient, ILogger<FIRSController> logger) : BaseApiController
{
    private readonly IFIRSHttpClient _firsClient = firsClient ?? throw new ArgumentNullException(nameof(firsClient));
    private readonly ILogger<FIRSController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #region SampleTestCode
  //  [HttpGet("downloadsample")]
  //  [SwaggerOperation(
  //    Summary = "Download invoice",
  //    Description = "Downloads Invoice"
  //)]
  //  [SwaggerResponse(200, "Invoice downloaded successfully", typeof(ApiResponse<object>))]
  //  [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
  //  [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
  //  [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
  //  [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
  //  public async Task<IActionResult> DownloadSampleInvoice(
  //   string irn,
  //    CancellationToken cancellationToken = default)
  //  {
  //      var downloadedInvoice = await _firsClient.DownloadInvoiceAsync(irn, cancellationToken);

  //      var apiKey = "306f9ed6";

  //      if (string.IsNullOrEmpty(apiKey))
  //          throw new BadRequestException("Decryption Key Not Found");

  //      if (downloadedInvoice is null)
  //          return NotFound();

  //      var decryptInvoice = AesDecryptionServiceAlt.Decrypt(
  //          Encoding.UTF8.GetBytes(downloadedInvoice.Data!.Pub.Trim() + apiKey),
  //          AesDecryptionServiceAlt.HexStringToByteArray(downloadedInvoice.Data.IvHex.Trim()),
  //          downloadedInvoice.Data!.Data.Trim());
  //      return Ok(decryptInvoice);
  //  }

    #endregion
}
